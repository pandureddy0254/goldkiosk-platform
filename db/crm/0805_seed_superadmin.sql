-- ============================================================================
-- 0805_seed_superadmin.sql
-- Bootstrap superadmin login. UNLIKE 0800 (demo seed, local/SIT only), this is
-- intentionally NOT guarded by database name: every environment needs the
-- superadmin so someone can sign in and create CRM members.
--
-- DEV-PHASE credentials (rotate before production):
--   email:    ngeller@goldkiosk.com
--   password: AiKiosk135#
--
-- Password is bcrypt-hashed in the DB via pgcrypto (same scheme as 0800 and as
-- crm.create_member), so the app verifies it with `password_hash = crypt(pw,
-- password_hash)` and the cleartext never leaves this file.
--
-- Idempotent: re-running re-asserts the role + dev password (so the documented
-- credentials always work after an apply during development).
-- ============================================================================

\echo '-- 0805 seed superadmin (all environments) --'

INSERT INTO crm.profiles (id, full_name, email, role, password_hash)
VALUES (
  '00000000-0000-0000-0000-0000000000ff',
  'Nakia Geller',
  'ngeller@goldkiosk.com',
  'superadmin',
  public.crypt('AiKiosk135#', public.gen_salt('bf'))
)
ON CONFLICT (email) DO UPDATE
  SET full_name     = excluded.full_name,
      role          = 'superadmin',
      password_hash = excluded.password_hash,
      updated_at    = now();

\echo 'ok: 0805 superadmin seeded (ngeller@goldkiosk.com).'
