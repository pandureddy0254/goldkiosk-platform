-- ============================================================================
-- 0003_create_roles_and_grants.sql
-- Creates the four CRM enterprise roles + per-schema USAGE/CREATE grants +
-- default privileges so future objects auto-inherit the right rights.
-- Idempotent. Safe to re-run.
--
-- Roles (cluster-global; prefixed gk_crm_ so they never collide with the
-- partner Admin Dashboard's gk_* roles if both DBs share a cluster):
--   gk_crm_owner     - DDL on every schema. Humans use this during deployments.
--   gk_crm_app       - DML on every schema; subject to RLS. The web app uses this.
--   gk_crm_readonly  - SELECT only. BI / reporting.
--   gk_crm_backend   - DML + BYPASSRLS. Background jobs + owner of SECURITY
--                      DEFINER functions. Replaces Supabase service_role.
-- ============================================================================

\echo '------------------------------------------------------------------------'
\echo '0003 - Roles and base grants'
\echo '------------------------------------------------------------------------'

-- --- Create roles (NOLOGIN groups; real logins are GRANTed the role) --------
DO $$ BEGIN CREATE ROLE gk_crm_owner    NOLOGIN; EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE ROLE gk_crm_app      NOLOGIN; EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE ROLE gk_crm_readonly NOLOGIN; EXCEPTION WHEN duplicate_object THEN NULL; END $$;
DO $$ BEGIN CREATE ROLE gk_crm_backend  NOLOGIN BYPASSRLS; EXCEPTION WHEN duplicate_object THEN NULL; END $$;

\echo '  ok: 4 roles created (or already existed)'

-- --- USAGE grants per schema ------------------------------------------------
GRANT USAGE ON SCHEMA crm, billing, audit
  TO gk_crm_owner, gk_crm_app, gk_crm_readonly, gk_crm_backend;

-- --- Owner gets CREATE for DDL ----------------------------------------------
GRANT CREATE ON SCHEMA crm, billing, audit TO gk_crm_owner;

\echo '  ok: USAGE/CREATE grants applied'

-- --- Default privileges so future objects auto-inherit role rights ----------
DO $$
DECLARE
  schema_name text;
BEGIN
  FOREACH schema_name IN ARRAY ARRAY['crm','billing','audit']
  LOOP
    -- gk_crm_app + gk_crm_backend: full DML on future tables
    EXECUTE format(
      'ALTER DEFAULT PRIVILEGES FOR ROLE gk_crm_owner IN SCHEMA %I
         GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO gk_crm_app, gk_crm_backend', schema_name);
    -- gk_crm_readonly: SELECT only
    EXECUTE format(
      'ALTER DEFAULT PRIVILEGES FOR ROLE gk_crm_owner IN SCHEMA %I
         GRANT SELECT ON TABLES TO gk_crm_readonly', schema_name);
    -- sequences (identity columns)
    EXECUTE format(
      'ALTER DEFAULT PRIVILEGES FOR ROLE gk_crm_owner IN SCHEMA %I
         GRANT USAGE, SELECT ON SEQUENCES TO gk_crm_app, gk_crm_backend', schema_name);
    EXECUTE format(
      'ALTER DEFAULT PRIVILEGES FOR ROLE gk_crm_owner IN SCHEMA %I
         GRANT SELECT ON SEQUENCES TO gk_crm_readonly', schema_name);
    -- EXECUTE on functions: backend can run everything. App is granted
    -- per-function in 01xx/03xx so service-only RPCs stay closed to the app.
    EXECUTE format(
      'ALTER DEFAULT PRIVILEGES FOR ROLE gk_crm_owner IN SCHEMA %I
         GRANT EXECUTE ON FUNCTIONS TO gk_crm_backend', schema_name);
  END LOOP;
END $$;

\echo '  ok: default privileges set for future objects'

-- --- Local-dev convenience: let the connecting superuser act as the roles ---
-- In Azure/RDS this is replaced by GRANTing gk_crm_app to the managed-identity
-- principal in the deployment template - never via this file.
DO $$ BEGIN
  IF current_database() LIKE 'goldkiosk_crm_local%' THEN
    EXECUTE format('GRANT gk_crm_owner, gk_crm_app, gk_crm_backend TO %I', current_user);
    RAISE NOTICE '  ok: % granted gk_crm_owner/app/backend (local-dev convenience)', current_user;
  END IF;
END $$;

-- --- Verification -----------------------------------------------------------
\echo ''
\echo '-- Roles --'
SELECT rolname, rolbypassrls, rolcanlogin
FROM pg_roles WHERE rolname LIKE 'gk_crm_%' ORDER BY rolname;

\echo ''
\echo 'ok: 0003 complete.'
