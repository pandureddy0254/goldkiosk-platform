-- ============================================================================
-- 0304_functions_members.sql
-- Member administration RPC (schema crm). SECURITY DEFINER; re-owned to
-- gk_crm_backend in 0700 so it can INSERT into crm.profiles (which has no app
-- INSERT policy — all member creation flows through here).
--
--   crm.create_member - superadmin-only: create a new CRM login (sales_manager
--                        or sales_rep). The password is bcrypt-hashed in the DB
--                        via pgcrypto, matching how the seed + superadmin are
--                        stored, so the app never has to hash/transport it.
-- ============================================================================

\echo '-- 0304 member admin RPC --'

-- p_email is text (not citext) so the .NET call create_member(text,text,text,text,text)
-- resolves — text->citext isn't an implicit cast for function resolution. The
-- email goes into a citext column (assignment cast) and the dup-check casts to citext.
DROP FUNCTION IF EXISTS crm.create_member(text, citext, text, text, text);

CREATE OR REPLACE FUNCTION crm.create_member(
  p_full_name  text,
  p_email      text,
  p_role       text,
  p_password   text,
  p_avatar_url text DEFAULT NULL
) RETURNS uuid
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
DECLARE
  v_actor    uuid := crm.current_user_id();
  v_new_id   uuid;
BEGIN
  -- Only the superadmin may create new CRM logins.
  IF NOT crm.is_superadmin() THEN
    RAISE EXCEPTION 'only the superadmin may create CRM members' USING ERRCODE = '42501';
  END IF;

  -- Members are sales staff only. The superadmin is bootstrap-seeded, never
  -- minted through this path.
  IF p_role NOT IN ('sales_manager','sales_rep') THEN
    RAISE EXCEPTION 'members must be sales_manager or sales_rep (got %)', p_role
      USING ERRCODE = '22023';
  END IF;

  IF p_full_name IS NULL OR length(btrim(p_full_name)) = 0 THEN
    RAISE EXCEPTION 'full_name is required' USING ERRCODE = '22023';
  END IF;
  IF p_email IS NULL OR length(btrim(p_email::text)) = 0 THEN
    RAISE EXCEPTION 'email is required' USING ERRCODE = '22023';
  END IF;
  IF p_password IS NULL OR length(p_password) < 8 THEN
    RAISE EXCEPTION 'password must be at least 8 characters' USING ERRCODE = '22023';
  END IF;

  IF EXISTS (SELECT 1 FROM crm.profiles WHERE email = p_email::citext) THEN
    RAISE EXCEPTION 'a member with email % already exists', p_email USING ERRCODE = '23505';
  END IF;

  INSERT INTO crm.profiles (full_name, email, role, avatar_url, password_hash)
  VALUES (btrim(p_full_name), p_email, p_role, p_avatar_url,
          public.crypt(p_password, public.gen_salt('bf')))
  RETURNING id INTO v_new_id;

  INSERT INTO audit.audit_log (actor_id, entity_type, entity_id, action, after)
  VALUES (v_actor, 'profile', v_new_id, 'create',
          jsonb_build_object('full_name', btrim(p_full_name), 'email', p_email::text, 'role', p_role));

  RETURN v_new_id;
END;
$$;

REVOKE ALL     ON FUNCTION crm.create_member(text, text, text, text, text) FROM PUBLIC;
GRANT  EXECUTE ON FUNCTION crm.create_member(text, text, text, text, text) TO gk_crm_app, gk_crm_backend;
COMMENT ON FUNCTION crm.create_member IS 'Superadmin-only: create a new CRM member (sales_manager|sales_rep). Password bcrypt-hashed via pgcrypto. Writes an audit.audit_log entry.';

\echo 'ok: 0304 member admin RPC done.'
