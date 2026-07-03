-- ============================================================================
-- 0301_functions_licensing.sql
-- Offline Ed25519 licensing RPCs (schema crm). This is the FINAL upstream
-- state: provision_tenant / reissue_activation_key / consume_activation_key
-- were dropped in favour of the offline model. C# (LicenseSigner) mints the
-- cleartext token + hash; the DB stores only the hash.
--   crm.ensure_tenant_for_partner  - C# calls this first to get the real tenant_id
--   crm.record_issued_license      - writes activation_keys row + audit, atomically
--   crm.revoke_license_key         - sales_manager-only revocation
-- ============================================================================

\echo '-- 0301 licensing RPCs --'

-- --- ensure_tenant_for_partner: upsert tenant row, return its id ------------
CREATE OR REPLACE FUNCTION crm.ensure_tenant_for_partner(
  p_partner_id  uuid,
  p_region_code text DEFAULT 'ae-1'
) RETURNS uuid
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
DECLARE
  v_tenant_id uuid;
  v_actor     uuid := crm.current_user_id();
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM crm.profiles
     WHERE crm.profiles.id = v_actor
       AND crm.profiles.role IN ('sales_manager','sales_rep')
  ) THEN
    RAISE EXCEPTION 'not authorised' USING ERRCODE = '42501';
  END IF;

  INSERT INTO crm.tenants (partner_id, region_code, provisioning_status)
  VALUES (p_partner_id, p_region_code, 'succeeded')
  ON CONFLICT (partner_id) DO UPDATE
    SET region_code         = excluded.region_code,
        provisioning_status = 'succeeded',
        updated_at          = now()
  RETURNING crm.tenants.id INTO v_tenant_id;

  RETURN v_tenant_id;
END;
$$;

REVOKE ALL     ON FUNCTION crm.ensure_tenant_for_partner(uuid, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION crm.ensure_tenant_for_partner(uuid, text) TO gk_crm_app, gk_crm_backend;

-- --- record_issued_license: write activation_keys row + audit, atomically ---
-- OUT params are named out_* to avoid the 42702 column-ambiguity that bit the
-- Supabase chain; every column reference is schema-qualified.
DROP FUNCTION IF EXISTS crm.record_issued_license(uuid, text, text, citext, timestamptz, text);
CREATE OR REPLACE FUNCTION crm.record_issued_license(
  p_partner_id      uuid,
  p_key_prefix      text,
  p_key_hash        text,
  p_issued_to_email text,   -- text (not citext) so .NET callers resolve; citext column takes it by assignment
  p_expires_at      timestamptz,
  p_region_code     text DEFAULT 'ae-1'
) RETURNS TABLE (
  out_tenant_id         uuid,
  out_activation_key_id uuid
)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
DECLARE
  v_tenant_id uuid;
  v_key_id    uuid;
  v_actor     uuid := crm.current_user_id();
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM crm.profiles
     WHERE crm.profiles.id = v_actor
       AND crm.profiles.role IN ('sales_manager','sales_rep')
  ) THEN
    RAISE EXCEPTION 'not authorised' USING ERRCODE = '42501';
  END IF;

  INSERT INTO crm.tenants (partner_id, region_code, provisioning_status)
  VALUES (p_partner_id, p_region_code, 'succeeded')
  ON CONFLICT (partner_id) DO UPDATE
    SET region_code         = excluded.region_code,
        provisioning_status = 'succeeded',
        updated_at          = now()
  RETURNING crm.tenants.id INTO v_tenant_id;

  -- Mark any previously-live key for this tenant as superseded.
  UPDATE crm.activation_keys ak
     SET revoked_at     = now(),
         revoked_reason = 'superseded by record_issued_license'
   WHERE ak.tenant_id   = v_tenant_id
     AND ak.consumed_at IS NULL
     AND ak.revoked_at  IS NULL;

  INSERT INTO crm.activation_keys
        (tenant_id, key_prefix, key_hash, issued_to_email, expires_at)
  VALUES (v_tenant_id, p_key_prefix, p_key_hash, p_issued_to_email, p_expires_at)
  RETURNING crm.activation_keys.id INTO v_key_id;

  -- Offline tokens take effect on issuance, not on a first activation back-call.
  UPDATE crm.partners p
     SET tenant_status = 'active', updated_at = now()
   WHERE p.id = p_partner_id AND p.tenant_status <> 'active';

  INSERT INTO audit.audit_log (actor_id, entity_type, entity_id, action, after)
  VALUES (v_actor, 'tenant', v_tenant_id, 'provision',
          jsonb_build_object('partner_id', p_partner_id, 'admin_email', p_issued_to_email,
                             'key_prefix', p_key_prefix, 'model', 'offline_ed25519'));

  RETURN QUERY SELECT v_tenant_id, v_key_id;
END;
$$;

REVOKE ALL     ON FUNCTION crm.record_issued_license(uuid, text, text, text, timestamptz, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION crm.record_issued_license(uuid, text, text, text, timestamptz, text) TO gk_crm_app, gk_crm_backend;

-- --- revoke_license_key: sales_manager-only ---------------------------------
CREATE OR REPLACE FUNCTION crm.revoke_license_key(
  p_key_prefix text,
  p_reason     text
) RETURNS integer
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
DECLARE
  v_actor uuid := crm.current_user_id();
  v_n     integer;
BEGIN
  IF NOT crm.is_sales_manager() THEN
    RAISE EXCEPTION 'only sales_manager may revoke license keys' USING ERRCODE = '42501';
  END IF;

  UPDATE crm.activation_keys
     SET revoked_at     = now(),
         revoked_reason = COALESCE(p_reason, 'manual revocation')
   WHERE key_prefix = p_key_prefix
     AND revoked_at IS NULL
     AND consumed_at IS NULL;
  GET DIAGNOSTICS v_n = ROW_COUNT;

  INSERT INTO audit.audit_log (actor_id, entity_type, action, after)
  VALUES (v_actor, 'activation_key', 'revoke_key',
          jsonb_build_object('key_prefix', p_key_prefix, 'reason', p_reason, 'rows', v_n));

  RETURN v_n;
END;
$$;

REVOKE ALL     ON FUNCTION crm.revoke_license_key(text, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION crm.revoke_license_key(text, text) TO gk_crm_app, gk_crm_backend;

\echo 'ok: 0301 licensing RPCs done.'
