-- ============================================================================
-- 0303_functions_integrations.sql
-- Stripe + SES dispatch RPCs (schema billing). Called by the background jobs
-- (the .NET webhook handlers / SES sender that replace the Supabase Edge
-- Functions). All backend-only; SECURITY DEFINER; re-owned to gk_crm_backend
-- in 0700.
-- ============================================================================

\echo '-- 0303 integration RPCs --'

-- --- stripe_apply_invoice_paid (invoice.paid) -------------------------------
-- Idempotency lives INSIDE this function: the stripe_event_log claim and the
-- renewal it triggers run in ONE transaction. The old design logged the event
-- in the .NET handler (transaction A) and renewed here (transaction B); a crash
-- between them left the event logged but the renewal lost, and the next Stripe
-- retry was rejected by the UNIQUE(stripe_event_id) constraint with no recovery
-- path. Now a crash rolls back BOTH the claim and the renewal, so the retry
-- re-claims and re-processes cleanly. The .NET webhook handler must therefore
-- pass the event id/type/payload here and must NOT pre-insert into
-- stripe_event_log for invoice.paid.
--
-- Signature changed (event id/type/api_version/payload added); drop the old
-- 4-arg overload so CREATE OR REPLACE doesn't leave two functions behind.
DROP FUNCTION IF EXISTS billing.stripe_apply_invoice_paid(text, text, text, timestamptz);

CREATE OR REPLACE FUNCTION billing.stripe_apply_invoice_paid(
  p_stripe_event_id        text,
  p_stripe_subscription_id text,
  p_stripe_invoice_id      text,
  p_stripe_payment_id      text,
  p_paid_at                timestamptz,
  p_event_type             text  DEFAULT 'invoice.paid',
  p_api_version            text  DEFAULT NULL,
  p_payload                jsonb DEFAULT '{}'::jsonb
) RETURNS timestamptz
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, crm, public, pg_temp
AS $$
DECLARE
  v_sub_id   uuid;
  v_new_end  timestamptz;
  v_inserted integer;
BEGIN
  -- Atomic claim: log the event in the SAME transaction as the renewal.
  INSERT INTO billing.stripe_event_log (stripe_event_id, event_type, api_version, payload)
  VALUES (p_stripe_event_id, p_event_type, p_api_version, COALESCE(p_payload, '{}'::jsonb))
  ON CONFLICT (stripe_event_id) DO NOTHING;
  GET DIAGNOSTICS v_inserted = ROW_COUNT;

  IF v_inserted = 0 THEN
    -- An earlier delivery already committed this event. No-op: return the live end.
    SELECT current_period_end INTO v_new_end
      FROM billing.subscriptions WHERE external_subscription_id = p_stripe_subscription_id;
    RETURN v_new_end;
  END IF;

  SELECT id INTO v_sub_id FROM billing.subscriptions
   WHERE external_subscription_id = p_stripe_subscription_id;

  IF v_sub_id IS NULL THEN
    -- Roll the whole transaction back (incl. the claim) so a later retry — after
    -- stripe_link_subscription has run — can re-process this event.
    RAISE EXCEPTION 'no local subscription with external_subscription_id = %', p_stripe_subscription_id
      USING ERRCODE = '42P01';
  END IF;

  v_new_end := billing.renew_subscription(
    p_subscription_id     => v_sub_id,
    p_paid_at             => p_paid_at,
    p_external_invoice_id => p_stripe_invoice_id,
    p_external_payment_id => p_stripe_payment_id
  );

  UPDATE billing.stripe_event_log SET processed_at = now()
   WHERE stripe_event_id = p_stripe_event_id;

  RETURN v_new_end;
END;
$$;

REVOKE ALL     ON FUNCTION billing.stripe_apply_invoice_paid(text, text, text, text, timestamptz, text, text, jsonb) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.stripe_apply_invoice_paid(text, text, text, text, timestamptz, text, text, jsonb) TO gk_crm_backend;

-- --- stripe_apply_invoice_payment_failed (invoice.payment_failed) -----------
CREATE OR REPLACE FUNCTION billing.stripe_apply_invoice_payment_failed(
  p_stripe_subscription_id text,
  p_stripe_invoice_id      text,
  p_failure_reason         text
) RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, crm, public, pg_temp
AS $$
DECLARE
  v_sub_id  uuid;
  v_partner uuid;
BEGIN
  SELECT id, partner_id INTO v_sub_id, v_partner FROM billing.subscriptions
   WHERE external_subscription_id = p_stripe_subscription_id;

  IF v_sub_id IS NULL THEN RETURN; END IF;

  UPDATE billing.subscriptions
     SET status = 'past_due', updated_at = now()
   WHERE id = v_sub_id AND status <> 'cancelled';

  INSERT INTO audit.audit_log (entity_type, entity_id, action, after)
  VALUES ('partner', v_partner, 'update',
          jsonb_build_object('subscription_id', v_sub_id, 'event', 'invoice.payment_failed',
                             'stripe_invoice', p_stripe_invoice_id, 'reason', p_failure_reason));
END;
$$;

REVOKE ALL     ON FUNCTION billing.stripe_apply_invoice_payment_failed(text, text, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.stripe_apply_invoice_payment_failed(text, text, text) TO gk_crm_backend;

-- --- stripe_apply_subscription_cancelled (customer.subscription.deleted) -----
CREATE OR REPLACE FUNCTION billing.stripe_apply_subscription_cancelled(
  p_stripe_subscription_id text,
  p_cancelled_at           timestamptz DEFAULT now()
) RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, crm, public, pg_temp
AS $$
BEGIN
  UPDATE billing.subscriptions
     SET status = 'cancelled',
         cancelled_at = p_cancelled_at,
         cancellation_effective_at = p_cancelled_at,
         cancellation_reason = COALESCE(cancellation_reason, 'stripe.customer.subscription.deleted'),
         auto_renew = false,
         updated_at = now()
   WHERE external_subscription_id = p_stripe_subscription_id;
END;
$$;

REVOKE ALL     ON FUNCTION billing.stripe_apply_subscription_cancelled(text, timestamptz) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.stripe_apply_subscription_cancelled(text, timestamptz) TO gk_crm_backend;

-- --- stripe_link_subscription (customer.subscription.created/.updated) -------
CREATE OR REPLACE FUNCTION billing.stripe_link_subscription(
  p_stripe_subscription_id text,
  p_stripe_customer_id     text,
  p_current_period_end     timestamptz,
  p_status                 text                  -- raw Stripe status
) RETURNS uuid
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, crm, public, pg_temp
AS $$
DECLARE
  v_sub_id       uuid;
  v_local_status text;
BEGIN
  SELECT id INTO v_sub_id FROM billing.subscriptions
   WHERE external_subscription_id = p_stripe_subscription_id;

  IF v_sub_id IS NULL THEN
    SELECT id INTO v_sub_id FROM billing.subscriptions
     WHERE external_customer_id = p_stripe_customer_id
       AND external_subscription_id IS NULL
     ORDER BY created_at DESC LIMIT 1;
  END IF;

  IF v_sub_id IS NULL THEN
    RAISE EXCEPTION 'no local subscription found for stripe customer %', p_stripe_customer_id
      USING ERRCODE = '42P01';
  END IF;

  v_local_status := CASE p_status
    WHEN 'trialing'          THEN 'trialing'
    WHEN 'active'            THEN 'active'
    WHEN 'past_due'          THEN 'past_due'
    WHEN 'unpaid'            THEN 'past_due'
    WHEN 'canceled'          THEN 'cancelled'
    WHEN 'incomplete'        THEN 'trialing'
    WHEN 'incomplete_expired' THEN 'expired'
    ELSE 'active'
  END;

  UPDATE billing.subscriptions
     SET external_subscription_id = p_stripe_subscription_id,
         current_period_end       = p_current_period_end,
         status                   = v_local_status,
         updated_at               = now()
   WHERE id = v_sub_id;

  RETURN v_sub_id;
END;
$$;

REVOKE ALL     ON FUNCTION billing.stripe_link_subscription(text, text, timestamptz, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.stripe_link_subscription(text, text, timestamptz, text) TO gk_crm_backend;

-- --- stripe_mark_event_processed --------------------------------------------
CREATE OR REPLACE FUNCTION billing.stripe_mark_event_processed(
  p_stripe_event_id text,
  p_error           text DEFAULT NULL
) RETURNS void
LANGUAGE sql
SECURITY DEFINER
SET search_path = billing, public, pg_temp
AS $$
  UPDATE billing.stripe_event_log
     SET processed_at = now(), error = p_error
   WHERE stripe_event_id = p_stripe_event_id;
$$;

REVOKE ALL     ON FUNCTION billing.stripe_mark_event_processed(text, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.stripe_mark_event_processed(text, text) TO gk_crm_backend;

-- --- ses_apply_delivery_event (SNS bounce/complaint/delivery) ---------------
CREATE OR REPLACE FUNCTION billing.ses_apply_delivery_event(
  p_ses_message_id text,
  p_event_type     text,                -- Delivery / Bounce / Complaint / Open / Click / Reject
  p_payload        jsonb,
  p_event_at       timestamptz DEFAULT now()
) RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, crm, public, pg_temp
AS $$
DECLARE v_new_status text;
BEGIN
  v_new_status := CASE lower(p_event_type)
    WHEN 'delivery'  THEN 'sent'
    WHEN 'bounce'    THEN 'bounced'
    WHEN 'complaint' THEN 'suppressed'
    WHEN 'open'      THEN 'opened'
    WHEN 'click'     THEN 'opened'
    WHEN 'reject'    THEN 'failed'
    ELSE NULL
  END;

  IF v_new_status IS NOT NULL THEN
    UPDATE billing.renewal_reminders
       SET delivery_status = v_new_status,
           failure_reason  = CASE
             WHEN lower(p_event_type) = 'bounce'    THEN COALESCE(p_payload #>> '{bounce,bounceType}', 'bounce')
             WHEN lower(p_event_type) = 'complaint' THEN 'complaint'
             WHEN lower(p_event_type) = 'reject'    THEN COALESCE(p_payload ->> 'reject', 'reject')
             ELSE failure_reason
           END
     WHERE external_message_id = p_ses_message_id;
  END IF;

  UPDATE billing.ses_message_log
     SET last_event = p_event_type, last_event_at = p_event_at, last_event_payload = p_payload
   WHERE message_id = p_ses_message_id;
END;
$$;

REVOKE ALL     ON FUNCTION billing.ses_apply_delivery_event(text, text, jsonb, timestamptz) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.ses_apply_delivery_event(text, text, jsonb, timestamptz) TO gk_crm_backend;

-- --- ses_record_send --------------------------------------------------------
DROP FUNCTION IF EXISTS billing.ses_record_send(text, uuid, citext, text, text, text);
CREATE OR REPLACE FUNCTION billing.ses_record_send(
  p_message_id        text,
  p_reminder_id       uuid,
  p_recipient         text,   -- text (not citext) so callers resolve; citext column takes it by assignment
  p_template_key      text,
  p_subject           text,
  p_configuration_set text
) RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, public, pg_temp
AS $$
BEGIN
  INSERT INTO billing.ses_message_log (
    message_id, reminder_id, recipient_email, template_key, subject, configuration_set
  ) VALUES (
    p_message_id, p_reminder_id, p_recipient, p_template_key, p_subject, p_configuration_set
  );
END;
$$;

REVOKE ALL     ON FUNCTION billing.ses_record_send(text, uuid, text, text, text, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.ses_record_send(text, uuid, text, text, text, text) TO gk_crm_backend;

\echo 'ok: 0303 integration RPCs done.'
