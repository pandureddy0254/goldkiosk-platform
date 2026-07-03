-- ============================================================================
-- 0302_functions_billing.sql
-- Subscription lifecycle RPCs (schema billing). SECURITY DEFINER; re-owned to
-- gk_crm_backend in 0700.
--   billing.start_subscription          - onboarding wizard           (app + backend)
--   billing.renew_subscription          - payment-provider webhook     (app + backend)
--   billing.cancel_subscription         - CRM Subscriptions screen     (app + backend)
--   billing.schedule_renewal_reminders  - cron                         (backend only)
--   billing.mark_reminder_sent          - SES sender                   (backend only)
--   billing.mark_past_due_subscriptions - cron                         (backend only)
-- ============================================================================

\echo '-- 0302 billing RPCs --'

-- --- add_billing_cycle: period anchor + one billing cycle -------------------
-- Single source of truth for "next period end". Raises on a NULL or
-- unrecognised cycle instead of returning NULL — otherwise a missed CASE branch
-- (or a future enum value like 'weekly') would feed NULL into the NOT NULL
-- current_period_end column and surface as a confusing constraint violation
-- well away from the real cause. STABLE (not IMMUTABLE): timestamptz + month/
-- year intervals depend on the session TimeZone.
CREATE OR REPLACE FUNCTION billing.add_billing_cycle(
  p_from  timestamptz,
  p_cycle text
) RETURNS timestamptz
LANGUAGE plpgsql
STABLE
AS $$
DECLARE v_end timestamptz;
BEGIN
  IF p_from IS NULL THEN
    RAISE EXCEPTION 'add_billing_cycle: anchor timestamp must not be NULL' USING ERRCODE = '22004';
  END IF;

  v_end := CASE p_cycle
    WHEN 'monthly'   THEN p_from + interval '1 month'
    WHEN 'quarterly' THEN p_from + interval '3 months'
    WHEN 'annual'    THEN p_from + interval '1 year'
    WHEN 'biennial'  THEN p_from + interval '2 years'
    ELSE NULL
  END;

  IF v_end IS NULL THEN
    RAISE EXCEPTION 'add_billing_cycle: unsupported or NULL billing_cycle (%)', p_cycle
      USING ERRCODE = '22023';  -- invalid_parameter_value
  END IF;

  RETURN v_end;
END;
$$;

REVOKE ALL     ON FUNCTION billing.add_billing_cycle(timestamptz, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.add_billing_cycle(timestamptz, text) TO gk_crm_app, gk_crm_backend;

-- --- start_subscription -----------------------------------------------------
CREATE OR REPLACE FUNCTION billing.start_subscription(
  p_partner_id     uuid,
  p_plan_code      text,
  p_amount         numeric,
  p_currency_code  text,
  p_billing_cycle  text,
  p_starts_at      timestamptz        DEFAULT now(),
  p_trial_days     int                DEFAULT 0,
  p_payment_method text               DEFAULT 'bank_transfer'
) RETURNS uuid
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, crm, public, pg_temp
AS $$
DECLARE
  v_sub_id     uuid;
  v_period_end timestamptz;
  v_actor      uuid := crm.current_user_id();
  v_currency   text := COALESCE(p_currency_code, 'USD');
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM crm.profiles
     WHERE id = v_actor AND role IN ('sales_manager','sales_rep')
  ) THEN
    RAISE EXCEPTION 'not authorised' USING ERRCODE = '42501';
  END IF;

  v_period_end := billing.add_billing_cycle(p_starts_at, p_billing_cycle);

  INSERT INTO billing.subscriptions (
    partner_id, plan_code, annual_amount, currency_code, billing_cycle,
    status, payment_method, started_at, current_period_start, current_period_end, trial_ends_at
  ) VALUES (
    p_partner_id, p_plan_code, p_amount, v_currency, p_billing_cycle,
    CASE WHEN p_trial_days > 0 THEN 'trialing' ELSE 'active' END,
    p_payment_method, p_starts_at, p_starts_at, v_period_end,
    CASE WHEN p_trial_days > 0 THEN p_starts_at + (p_trial_days || ' days')::interval ELSE NULL END
  ) RETURNING id INTO v_sub_id;

  INSERT INTO billing.subscription_periods (
    subscription_id, period_start, period_end, amount, currency_code, status, invoiced_at
  ) VALUES (
    v_sub_id, p_starts_at, v_period_end, p_amount, v_currency, 'pending', now()
  );

  INSERT INTO audit.audit_log (actor_id, entity_type, entity_id, action, after)
  VALUES (v_actor, 'partner', p_partner_id, 'create',
          jsonb_build_object('subscription_id', v_sub_id, 'plan', p_plan_code,
                             'cycle', p_billing_cycle, 'amount', p_amount, 'currency', v_currency));

  RETURN v_sub_id;
END;
$$;

REVOKE ALL     ON FUNCTION billing.start_subscription(uuid, text, numeric, text, text, timestamptz, int, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.start_subscription(uuid, text, numeric, text, text, timestamptz, int, text) TO gk_crm_app, gk_crm_backend;

-- --- renew_subscription -----------------------------------------------------
CREATE OR REPLACE FUNCTION billing.renew_subscription(
  p_subscription_id     uuid,
  p_paid_at             timestamptz DEFAULT now(),
  p_external_invoice_id text        DEFAULT NULL,
  p_external_payment_id text        DEFAULT NULL
) RETURNS timestamptz
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, crm, public, pg_temp
AS $$
DECLARE
  v_sub     billing.subscriptions%rowtype;
  v_new_end timestamptz;
  v_actor   uuid := crm.current_user_id();
BEGIN
  SELECT * INTO v_sub FROM billing.subscriptions WHERE id = p_subscription_id FOR UPDATE;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'subscription % not found', p_subscription_id;
  END IF;

  -- Idempotency (Stripe delivers at-least-once): if this invoice has already
  -- been applied to a paid period, this is a retry. Return the current end
  -- WITHOUT advancing the period or inserting a duplicate — otherwise a second
  -- call would push current_period_end forward by an extra cycle and double-bill.
  IF p_external_invoice_id IS NOT NULL AND EXISTS (
    SELECT 1 FROM billing.subscription_periods
     WHERE subscription_id = p_subscription_id
       AND external_invoice_id = p_external_invoice_id
       AND status = 'paid'
  ) THEN
    RETURN v_sub.current_period_end;
  END IF;

  UPDATE billing.subscription_periods
     SET status = 'paid', paid_at = p_paid_at,
         external_invoice_id = COALESCE(p_external_invoice_id, external_invoice_id),
         external_payment_id = COALESCE(p_external_payment_id, external_payment_id)
   WHERE subscription_id = p_subscription_id
     AND period_end = v_sub.current_period_end
     AND status IN ('pending','refunded');

  v_new_end := billing.add_billing_cycle(v_sub.current_period_end, v_sub.billing_cycle);

  UPDATE billing.subscriptions
     SET current_period_start = v_sub.current_period_end,
         current_period_end   = v_new_end,
         last_renewal_at      = p_paid_at,
         status               = 'active',
         updated_at           = now()
   WHERE id = p_subscription_id;

  INSERT INTO billing.subscription_periods (
    subscription_id, period_start, period_end, amount, currency_code, status, invoiced_at
  ) VALUES (
    p_subscription_id, v_sub.current_period_end, v_new_end,
    v_sub.annual_amount, v_sub.currency_code, 'pending', p_paid_at
  );

  INSERT INTO audit.audit_log (actor_id, entity_type, entity_id, action, after)
  VALUES (v_actor, 'partner', v_sub.partner_id, 'update',
          jsonb_build_object('subscription_id', p_subscription_id, 'renewed_through', v_new_end,
                             'external_invoice', p_external_invoice_id));

  RETURN v_new_end;
END;
$$;

REVOKE ALL     ON FUNCTION billing.renew_subscription(uuid, timestamptz, text, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.renew_subscription(uuid, timestamptz, text, text) TO gk_crm_app, gk_crm_backend;

-- --- cancel_subscription (sales_manager-only) -------------------------------
CREATE OR REPLACE FUNCTION billing.cancel_subscription(
  p_subscription_id uuid,
  p_reason          text,
  p_effective_at    timestamptz DEFAULT NULL   -- NULL = at end of current period
) RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, crm, public, pg_temp
AS $$
DECLARE
  v_sub   billing.subscriptions%rowtype;
  v_when  timestamptz;
  v_actor uuid := crm.current_user_id();
BEGIN
  IF NOT crm.is_sales_manager() THEN
    RAISE EXCEPTION 'only sales_manager may cancel subscriptions' USING ERRCODE = '42501';
  END IF;

  SELECT * INTO v_sub FROM billing.subscriptions WHERE id = p_subscription_id;
  IF NOT FOUND THEN RAISE EXCEPTION 'subscription % not found', p_subscription_id; END IF;

  v_when := COALESCE(p_effective_at, v_sub.current_period_end);

  UPDATE billing.subscriptions
     SET cancelled_at = now(),
         cancellation_reason = p_reason,
         cancellation_effective_at = v_when,
         auto_renew = false,
         status = CASE WHEN v_when <= now() THEN 'cancelled' ELSE status END,
         updated_at = now()
   WHERE id = p_subscription_id;

  INSERT INTO audit.audit_log (actor_id, entity_type, entity_id, action, after)
  VALUES (v_actor, 'partner', v_sub.partner_id, 'update',
          jsonb_build_object('subscription_id', p_subscription_id,
                             'cancelled_effective_at', v_when, 'reason', p_reason));
END;
$$;

REVOKE ALL     ON FUNCTION billing.cancel_subscription(uuid, text, timestamptz) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.cancel_subscription(uuid, text, timestamptz) TO gk_crm_app, gk_crm_backend;

-- --- schedule_renewal_reminders (cron) --------------------------------------
CREATE OR REPLACE FUNCTION billing.schedule_renewal_reminders()
RETURNS int
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, crm, public, pg_temp
AS $$
DECLARE v_inserted int := 0;
BEGIN
  WITH windows(kind, offset_days) AS (
    VALUES
      ('T-60'::text, 60),
      ('T-30'::text, 30),
      ('T-14'::text, 14),
      ('T-7'::text,  7),
      ('T-1'::text,  1),
      ('T+1'::text, -1),
      ('T+7'::text, -7),
      ('T+30'::text,-30)
  ),
  candidates AS (
    SELECT s.id AS subscription_id,
           w.kind,
           (s.current_period_end - (w.offset_days || ' days')::interval)::date AS scheduled_for,
           COALESCE((SELECT primary_admin_email FROM crm.partners p WHERE p.id = s.partner_id), 'unknown@example.com')::citext AS recipient_email
      FROM billing.subscriptions s, windows w
     WHERE s.auto_renew = true
       AND s.status IN ('active','trialing','past_due')
       AND (s.current_period_end - (w.offset_days || ' days')::interval)::date = current_date
  )
  INSERT INTO billing.renewal_reminders (subscription_id, kind, scheduled_for, recipient_email)
  SELECT subscription_id, kind, scheduled_for, recipient_email FROM candidates
  ON CONFLICT (subscription_id, kind, scheduled_for) DO NOTHING;

  GET DIAGNOSTICS v_inserted = ROW_COUNT;
  RETURN v_inserted;
END;
$$;

REVOKE ALL     ON FUNCTION billing.schedule_renewal_reminders() FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.schedule_renewal_reminders() TO gk_crm_backend;

-- --- mark_reminder_sent (SES sender) ----------------------------------------
CREATE OR REPLACE FUNCTION billing.mark_reminder_sent(
  p_reminder_id         uuid,
  p_delivery_status     text,
  p_external_message_id text DEFAULT NULL,
  p_failure_reason      text DEFAULT NULL
) RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, crm, public, pg_temp
AS $$
BEGIN
  UPDATE billing.renewal_reminders
     SET delivery_status     = p_delivery_status,
         external_message_id = COALESCE(p_external_message_id, external_message_id),
         failure_reason      = p_failure_reason,
         sent_at             = CASE WHEN p_delivery_status IN ('sent','opened') THEN now() ELSE sent_at END
   WHERE id = p_reminder_id;
END;
$$;

REVOKE ALL     ON FUNCTION billing.mark_reminder_sent(uuid, text, text, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.mark_reminder_sent(uuid, text, text, text) TO gk_crm_backend;

-- --- mark_past_due_subscriptions (cron) -------------------------------------
CREATE OR REPLACE FUNCTION billing.mark_past_due_subscriptions()
RETURNS int
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = billing, crm, public, pg_temp
AS $$
DECLARE v_n int := 0;
BEGIN
  WITH bumped AS (
    UPDATE billing.subscriptions
       SET status = 'past_due', updated_at = now()
     WHERE status = 'active'
       AND current_period_end < now()
       AND EXISTS (
         SELECT 1 FROM billing.subscription_periods sp
          WHERE sp.subscription_id = subscriptions.id
            AND sp.period_end = subscriptions.current_period_end
            AND sp.status = 'pending'
       )
    RETURNING 1
  )
  SELECT count(*) INTO v_n FROM bumped;

  -- Suspend after 14 days past_due.
  UPDATE billing.subscriptions
     SET status = 'suspended', updated_at = now()
   WHERE status = 'past_due'
     AND current_period_end < now() - interval '14 days';

  RETURN v_n;
END;
$$;

REVOKE ALL     ON FUNCTION billing.mark_past_due_subscriptions() FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION billing.mark_past_due_subscriptions() TO gk_crm_backend;

-- --- pg_cron schedule (best-effort; gated on extension presence) ------------
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pg_cron') THEN
    PERFORM cron.schedule('gk-crm-schedule-renewal-reminders', '0 2 * * *',
                          $cron$ SELECT billing.schedule_renewal_reminders(); $cron$);
    PERFORM cron.schedule('gk-crm-mark-past-due', '0 3 * * *',
                          $cron$ SELECT billing.mark_past_due_subscriptions(); $cron$);
    RAISE NOTICE '  ok: pg_cron jobs scheduled (02:00 reminders, 03:00 past-due)';
  ELSE
    RAISE NOTICE '  pg_cron not installed - schedule billing.schedule_renewal_reminders() + billing.mark_past_due_subscriptions() externally.';
  END IF;
END $$;

\echo 'ok: 0302 billing RPCs done.'
