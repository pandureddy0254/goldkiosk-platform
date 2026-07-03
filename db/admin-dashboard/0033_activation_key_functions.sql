-- ============================================================================
-- 0033_activation_key_functions.sql
-- Two SECURITY DEFINER functions that let the AdminDashboard activate a new
-- primary admin without disclosing the tenant_id of the activation key to an
-- unauthenticated caller. Both functions run with elevated privilege (bypass
-- the activation_keys RLS policy) and apply strict input validation.
--
-- Flow:
--   1. App calls identity.validate_activation_key(p_key, p_email) — returns
--      tenant_id, key_id, owner_role_id on a clean match; raises on any
--      failure (bad hash, wrong email, expired, consumed, revoked).
--   2. App sets the tenant context to the returned tenant_id and creates the
--      AppUser via the normal ASP.NET Core Identity path.
--   3. App calls identity.consume_activation_key(p_key_id, p_user_id) inside
--      the same transaction to mark the key consumed.
--
-- Idempotent.
-- ============================================================================

\echo '── 0033 activation-key SECURITY DEFINER functions ──'

CREATE OR REPLACE FUNCTION identity.validate_activation_key(
    p_key   text,
    p_email public.domain_email,
    OUT o_tenant_id      uuid,
    OUT o_key_id         uuid,
    OUT o_owner_role_id  uuid,
    OUT o_key_prefix     citext
)
RETURNS RECORD
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = identity, tenancy, public, pg_temp
AS $$
DECLARE
    v_normalised text;
    v_hash       bytea;
BEGIN
    -- Normalise: uppercase, strip whitespace. Cleartext format must look like
    -- "AIKI-XXXX-XXXX-XXXX" (19 chars including dashes, hex groups).
    v_normalised := upper(replace(replace(coalesce(p_key, ''), ' ', ''), E'\t', ''));

    IF v_normalised !~ '^AIKI-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}$' THEN
        RAISE EXCEPTION 'invalid_activation_key_format'
            USING ERRCODE = '22023', HINT = 'Format must be AIKI-XXXX-XXXX-XXXX (hex).';
    END IF;

    v_hash := digest(v_normalised, 'sha256');

    SELECT k.id, k.tenant_id, r.id, k.key_prefix
      INTO o_key_id, o_tenant_id, o_owner_role_id, o_key_prefix
      FROM identity.activation_keys k
      JOIN identity.roles r
        ON r.tenant_id = k.tenant_id
       AND r.code      = 'owner'
       AND r.is_active = true
       AND r.deleted_at IS NULL
     WHERE k.key_hash = v_hash
       AND k.consumed_at IS NULL
       AND k.revoked_at  IS NULL
       AND k.expires_at  > now()
       AND lower(k.issued_to_email::text) = lower(p_email::text);

    IF o_key_id IS NULL THEN
        RAISE EXCEPTION 'invalid_or_expired_activation_key'
            USING ERRCODE = '22023',
                  HINT    = 'Key may be wrong, already used, revoked, expired, or the email may not match.';
    END IF;
END;
$$;

COMMENT ON FUNCTION identity.validate_activation_key(text, public.domain_email) IS
    'Validate an AIKI-XXXX-XXXX-XXXX activation key against (key, email). Returns '
    'tenant_id + key_id + owner_role_id on success; raises 22023 on any failure.';

CREATE OR REPLACE FUNCTION identity.consume_activation_key(
    p_key_id  uuid,
    p_user_id uuid
)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = identity, public, pg_temp
AS $$
BEGIN
    UPDATE identity.activation_keys
       SET consumed_at         = now(),
           consumed_by_user_id = p_user_id,
           updated_at          = now()
     WHERE id          = p_key_id
       AND consumed_at IS NULL
       AND revoked_at  IS NULL
       AND expires_at  > now();

    IF NOT FOUND THEN
        RAISE EXCEPTION 'activation_key_no_longer_valid'
            USING ERRCODE = '22023';
    END IF;
END;
$$;

COMMENT ON FUNCTION identity.consume_activation_key(uuid, uuid) IS
    'Mark an activation key consumed by a user. Raises if the key was already '
    'consumed, revoked, or expired in a race.';

-- Grant execute to the app role (gk_app) so the AdminDashboard can call these.
DO $$ BEGIN
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'gk_app') THEN
        GRANT EXECUTE ON FUNCTION identity.validate_activation_key(text, public.domain_email) TO gk_app;
        GRANT EXECUTE ON FUNCTION identity.consume_activation_key(uuid, uuid)                 TO gk_app;
    END IF;
END $$;

\echo '✓ 0033 done.'
