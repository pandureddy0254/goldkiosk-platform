-- ============================================================================
-- 0003_create_roles_and_grants.sql
-- Creates the five enterprise roles + per-schema USAGE grants.
-- Idempotent. Safe to re-run.
-- Target: goldkiosk_<env> (any env)
--
-- Roles (same names across SIT/UAT/Prod and local):
--   gk_owner         — DDL on every schema. Used by humans during deployments.
--   gk_app           — DML on every schema; subject to RLS. The app uses this.
--   gk_app_readonly  — SELECT only. For BI / reporting tools.
--   gk_backend       — DML + BYPASSRLS. Background workers, snapshots, retention.
--   gk_audit_reader  — SELECT on audit.* and compliance.*. Compliance officers.
-- ============================================================================

\echo '────────────────────────────────────────────────────────────────────────'
\echo '0003 — Roles and base grants'
\echo '────────────────────────────────────────────────────────────────────────'

-- ─── Create roles (NOLOGIN — they exist as groups; real logins are GRANTed) ─
-- In Azure Flexible Server the app will connect via Entra ID and be GRANTed
-- the gk_app role. Locally, we'll grant gk_app to the postgres superuser
-- (handled at the end of this file) so dev psql sessions can use it.

DO $$ BEGIN
  CREATE ROLE gk_owner NOLOGIN;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
  CREATE ROLE gk_app NOLOGIN;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
  CREATE ROLE gk_app_readonly NOLOGIN;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
  CREATE ROLE gk_backend NOLOGIN BYPASSRLS;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
  CREATE ROLE gk_audit_reader NOLOGIN;
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

\echo '  ✓ 5 roles created (or already existed)'

-- ─── USAGE grants per schema ────────────────────────────────────────────────
-- USAGE = role can "see" the schema and reference objects in it. Object-level
-- SELECT/INSERT/UPDATE/DELETE permissions are layered on top in 0070_grants.sql
-- (final pass after all tables exist).

GRANT USAGE ON SCHEMA audit, compliance, config, customer, doc, helpdesk,
                       identity, integration, kiosk, merchant, monitor, ops,
                       payment, pricing, store, tenancy, tx, voucher, reporting
  TO gk_owner, gk_app, gk_app_readonly, gk_backend;

-- Audit reader sees only audit and compliance.
GRANT USAGE ON SCHEMA audit, compliance TO gk_audit_reader;

\echo '  ✓ USAGE grants applied per role'

-- ─── Owner gets CREATE on every schema (for DDL) ────────────────────────────

GRANT CREATE ON SCHEMA audit, compliance, config, customer, doc, helpdesk,
                       identity, integration, kiosk, merchant, monitor, ops,
                       payment, pricing, store, tenancy, tx, voucher, reporting
  TO gk_owner;

\echo '  ✓ CREATE grants applied to gk_owner'

-- ─── Default privileges so future tables auto-inherit role rights ───────────
-- These run for every schema and say: "when gk_owner creates a table here,
-- automatically grant DML on it to gk_app, SELECT to gk_app_readonly, etc."

DO $$
DECLARE
  schema_name text;
BEGIN
  FOR schema_name IN
    SELECT unnest(ARRAY['audit','compliance','config','customer','doc','helpdesk',
                        'identity','integration','kiosk','merchant','monitor','ops',
                        'payment','pricing','store','tenancy','tx','voucher','reporting'])
  LOOP
    -- gk_app: full DML on tables
    EXECUTE format(
      'ALTER DEFAULT PRIVILEGES FOR ROLE gk_owner IN SCHEMA %I
         GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO gk_app', schema_name);
    -- gk_backend: full DML on tables
    EXECUTE format(
      'ALTER DEFAULT PRIVILEGES FOR ROLE gk_owner IN SCHEMA %I
         GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO gk_backend', schema_name);
    -- gk_app_readonly: SELECT only
    EXECUTE format(
      'ALTER DEFAULT PRIVILEGES FOR ROLE gk_owner IN SCHEMA %I
         GRANT SELECT ON TABLES TO gk_app_readonly', schema_name);
    -- USAGE on sequences (for serial/uuid-default-using-seq columns later)
    EXECUTE format(
      'ALTER DEFAULT PRIVILEGES FOR ROLE gk_owner IN SCHEMA %I
         GRANT USAGE, SELECT ON SEQUENCES TO gk_app, gk_backend', schema_name);
    EXECUTE format(
      'ALTER DEFAULT PRIVILEGES FOR ROLE gk_owner IN SCHEMA %I
         GRANT SELECT ON SEQUENCES TO gk_app_readonly', schema_name);
    -- EXECUTE on functions
    EXECUTE format(
      'ALTER DEFAULT PRIVILEGES FOR ROLE gk_owner IN SCHEMA %I
         GRANT EXECUTE ON FUNCTIONS TO gk_app, gk_backend', schema_name);
  END LOOP;

  -- Audit reader: SELECT-only default on future audit + compliance tables.
  ALTER DEFAULT PRIVILEGES FOR ROLE gk_owner IN SCHEMA audit
    GRANT SELECT ON TABLES TO gk_audit_reader;
  ALTER DEFAULT PRIVILEGES FOR ROLE gk_owner IN SCHEMA compliance
    GRANT SELECT ON TABLES TO gk_audit_reader;
END $$;

\echo '  ✓ default privileges set for future objects'

-- ─── Local convenience: let postgres superuser act as gk_app for dev work ───
-- In Azure, this is replaced by GRANTing gk_app to the managed-identity
-- principal in the Bicep template — never via this file.
-- For local dev only:

DO $$ BEGIN
  IF current_database() LIKE 'goldkiosk_local%' THEN
    GRANT gk_owner, gk_app, gk_backend TO postgres;
    RAISE NOTICE '  ✓ postgres granted gk_owner/gk_app/gk_backend (local-dev convenience)';
  END IF;
END $$;

-- ─── Verification ────────────────────────────────────────────────────────────
\echo ''
\echo '── Roles ──'
SELECT rolname, rolbypassrls, rolcanlogin
FROM pg_roles
WHERE rolname LIKE 'gk_%'
ORDER BY rolname;

\echo ''
\echo '── Schema USAGE grants (sample for tenancy) ──'
SELECT grantee, privilege_type
FROM information_schema.role_usage_grants
WHERE object_schema = 'tenancy'
ORDER BY grantee, privilege_type;

\echo ''
\echo '✓ 0003 complete.'
