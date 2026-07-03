-- ============================================================================
-- 0300_functions_pipeline.sql
-- Pipeline RPCs (schema crm). All SECURITY DEFINER + search_path-locked; the
-- function owner is reset to gk_crm_backend in 0700 so they bypass RLS.
--   crm.move_lead_stage
--   crm.mark_lead_won_and_create_partner
--   crm.list_activation_keys
-- ============================================================================

\echo '-- 0300 pipeline RPCs --'

-- --- move_lead_stage: atomic transition + optional note ---------------------
CREATE OR REPLACE FUNCTION crm.move_lead_stage(
  p_lead_id   uuid,
  p_new_stage text,
  p_note      text
) RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
DECLARE
  v_actor   uuid := crm.current_user_id();
  v_owner   uuid;
  v_deleted timestamptz;
BEGIN
  -- This RPC runs as gk_crm_backend (BYPASSRLS), so it must re-assert the same
  -- authorization the leads_app_update RLS policy would enforce: a sales_rep may
  -- move only a lead they own; a sales_manager may move any. Without this check
  -- any CRM user could stage any lead (incl. straight to 'won') — lead hijacking.
  SELECT owner_id, deleted_at INTO v_owner, v_deleted
    FROM crm.leads WHERE id = p_lead_id;

  IF NOT FOUND OR v_deleted IS NOT NULL THEN
    RAISE EXCEPTION 'lead % not found', p_lead_id USING ERRCODE = 'P0002';  -- no_data_found
  END IF;

  IF NOT (crm.is_sales_manager() OR v_owner = v_actor) THEN
    RAISE EXCEPTION 'not authorised to move lead %', p_lead_id USING ERRCODE = '42501';
  END IF;

  UPDATE crm.leads
     SET stage = p_new_stage, updated_at = now()
   WHERE id = p_lead_id AND deleted_at IS NULL;

  IF p_note IS NOT NULL AND length(btrim(p_note)) > 0 THEN
    INSERT INTO crm.lead_activities (lead_id, actor_id, activity_type, payload)
    VALUES (p_lead_id, v_actor, 'note', jsonb_build_object('text', p_note));
  END IF;
END;
$$;

REVOKE ALL     ON FUNCTION crm.move_lead_stage(uuid, text, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION crm.move_lead_stage(uuid, text, text) TO gk_crm_app, gk_crm_backend;

-- --- mark_lead_won_and_create_partner: lead -> partner in one transaction ----
-- p_primary_admin_email is text (not citext) so .NET callers resolve the function
-- (text->citext is not an implicit cast); it lands in a citext column by assignment.
DROP FUNCTION IF EXISTS crm.mark_lead_won_and_create_partner(uuid, text, jsonb, text, citext, text, integer);
CREATE OR REPLACE FUNCTION crm.mark_lead_won_and_create_partner(
  p_lead_id                  uuid,
  p_legal_name               text,
  p_billing                  jsonb,
  p_primary_admin_name       text,
  p_primary_admin_email      text,
  p_primary_admin_role_title text,
  p_initial_kiosk_count      integer
) RETURNS uuid
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
DECLARE
  v_partner_id uuid;
  v_actor      uuid := crm.current_user_id();
  v_region     text;
  v_owner      uuid;
  v_deleted    timestamptz;
BEGIN
  -- Same authorization gate as move_lead_stage: this DEFINER RPC bypasses RLS,
  -- so re-assert that the caller owns the lead (or is a manager) before turning
  -- it into a partner. Closes the second half of the lead-hijack chain — without
  -- it a rep could convert another rep's lead and claim the partner.
  SELECT region, owner_id, deleted_at INTO v_region, v_owner, v_deleted
    FROM crm.leads WHERE id = p_lead_id;

  IF NOT FOUND OR v_deleted IS NOT NULL THEN
    RAISE EXCEPTION 'lead % not found', p_lead_id USING ERRCODE = 'P0002';  -- no_data_found
  END IF;

  IF NOT (crm.is_sales_manager() OR v_owner = v_actor) THEN
    RAISE EXCEPTION 'not authorised to convert lead %', p_lead_id USING ERRCODE = '42501';
  END IF;

  UPDATE crm.leads SET stage = 'won', updated_at = now() WHERE id = p_lead_id;

  INSERT INTO crm.partners (
    legal_name, display_name, billing_address, region,
    primary_admin_name, primary_admin_email, primary_admin_role_title,
    initial_kiosk_count, lead_id
  ) VALUES (
    p_legal_name, p_legal_name, p_billing, v_region,
    p_primary_admin_name, p_primary_admin_email, p_primary_admin_role_title,
    p_initial_kiosk_count, p_lead_id
  ) RETURNING id INTO v_partner_id;

  INSERT INTO audit.audit_log (actor_id, entity_type, entity_id, action, after)
  VALUES (v_actor, 'partner', v_partner_id, 'create',
          jsonb_build_object('from_lead', p_lead_id, 'legal_name', p_legal_name));

  RETURN v_partner_id;
END;
$$;

REVOKE ALL     ON FUNCTION crm.mark_lead_won_and_create_partner(uuid, text, jsonb, text, text, text, integer) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION crm.mark_lead_won_and_create_partner(uuid, text, jsonb, text, text, text, integer) TO gk_crm_app, gk_crm_backend;

-- --- list_activation_keys: safe metadata (no key_hash), caller-scoped -------
CREATE OR REPLACE FUNCTION crm.list_activation_keys(p_tenant_id uuid DEFAULT NULL)
RETURNS TABLE (
  id              uuid,
  tenant_id       uuid,
  key_prefix      text,
  issued_to_email citext,
  issued_at       timestamptz,
  expires_at      timestamptz,
  consumed_at     timestamptz,
  revoked_at      timestamptz,
  revoked_reason  text
)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
  SELECT ak.id, ak.tenant_id, ak.key_prefix, ak.issued_to_email,
         ak.issued_at, ak.expires_at, ak.consumed_at, ak.revoked_at, ak.revoked_reason
    FROM crm.activation_keys ak
   WHERE (p_tenant_id IS NULL OR ak.tenant_id = p_tenant_id)
     AND (
       crm.is_sales_manager()
       OR EXISTS (
         SELECT 1
           FROM crm.tenants  t
           JOIN crm.partners p ON p.id = t.partner_id
           JOIN crm.leads    l ON l.id = p.lead_id
          WHERE t.id = ak.tenant_id
            AND l.owner_id = crm.current_user_id()
       )
     );
$$;

REVOKE ALL     ON FUNCTION crm.list_activation_keys(uuid) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION crm.list_activation_keys(uuid) TO gk_crm_app, gk_crm_backend;
COMMENT ON FUNCTION crm.list_activation_keys IS 'Activation-key metadata (no key_hash), scoped: managers see all; reps see keys for partners they own via the source lead.';

\echo 'ok: 0300 pipeline RPCs done.'
