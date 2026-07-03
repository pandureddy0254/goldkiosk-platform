-- ============================================================================
-- 0305_functions_auth.sql
-- Login verification RPC (schema crm). SECURITY DEFINER; re-owned to
-- gk_crm_backend in 0700 so it bypasses RLS — essential because at sign-in time
-- there is no user context yet, so the profiles RLS SELECT policy
-- (id = current_user_id() OR is_sales_manager()) would otherwise return nothing.
--
--   crm.verify_login - returns the matching profile (id, full_name, role) when
--                      the bcrypt password matches, or no row otherwise. The
--                      cleartext password is compared against password_hash via
--                      pgcrypto crypt(), so the app never hashes/stores it.
-- ============================================================================

\echo '-- 0305 auth RPC --'

-- p_email is text (not citext) so the .NET call verify_login(text, text) resolves
-- — text->citext is not an implicit cast during function resolution. The body
-- casts to citext for the case-insensitive match against the citext column.
DROP FUNCTION IF EXISTS crm.verify_login(citext, text);

CREATE OR REPLACE FUNCTION crm.verify_login(
  p_email    text,
  p_password text
) RETURNS TABLE (
  id        uuid,
  full_name text,
  role      text
)
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
DECLARE
  v_profile crm.profiles%ROWTYPE;
BEGIN
  SELECT p.* INTO v_profile
    FROM crm.profiles p
   WHERE p.email = p_email::citext
     AND p.password_hash IS NOT NULL
     AND p.password_hash = public.crypt(p_password, p.password_hash)
   LIMIT 1;

  IF v_profile.id IS NULL THEN
    RETURN;
  END IF;

  UPDATE crm.profiles
     SET last_signed_in_at = now()
   WHERE crm.profiles.id = v_profile.id;

  RETURN QUERY SELECT v_profile.id, v_profile.full_name, v_profile.role;
END;
$$;

REVOKE ALL     ON FUNCTION crm.verify_login(text, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION crm.verify_login(text, text) TO gk_crm_app, gk_crm_backend;
COMMENT ON FUNCTION crm.verify_login IS 'Returns (id, full_name, role) when the email + bcrypt password match an active profile; no row otherwise. SECURITY DEFINER so it works before any user context is set (sign-in).';

\echo 'ok: 0305 auth RPC done.'
