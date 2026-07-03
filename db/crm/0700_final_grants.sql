-- ============================================================================
-- 0700_final_grants.sql
-- Final pass over everything that already exists:
--   1. table/view/sequence DML grants for the four roles;
--   2. reassign every crm/billing function to gk_crm_backend so its
--      SECURITY DEFINER body runs with BYPASSRLS (the privileged-write path
--      the RPCs and business triggers depend on).
--
-- Per-function EXECUTE grants were applied in 01xx/03xx and are preserved
-- across the ownership change. Default privileges (0003) cover FUTURE objects;
-- this file covers what exists now (created by the deploy/superuser role, not
-- gk_crm_owner, so the default-privilege rules did not fire for them).
--
-- Reassigning ownership requires the running role to be a member of
-- gk_crm_backend (true for the local-dev superuser via 0003; in prod grant
-- gk_crm_backend to the deploy principal first).
-- ============================================================================

\echo '-- 0700 final grants + function owner reassignment --'

-- --- 1. table / view / sequence grants --------------------------------------
DO $$
DECLARE s text;
BEGIN
  FOREACH s IN ARRAY ARRAY['crm','billing','audit'] LOOP
    EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES    IN SCHEMA %I TO gk_crm_app, gk_crm_backend', s);
    EXECUTE format('GRANT SELECT                          ON ALL TABLES    IN SCHEMA %I TO gk_crm_readonly', s);
    EXECUTE format('GRANT USAGE, SELECT                   ON ALL SEQUENCES IN SCHEMA %I TO gk_crm_app, gk_crm_backend', s);
    EXECUTE format('GRANT SELECT                          ON ALL SEQUENCES IN SCHEMA %I TO gk_crm_readonly', s);
  END LOOP;
END $$;

\echo '  ok: table/view/sequence grants applied'

-- --- 2. reassign function ownership to gk_crm_backend (BYPASSRLS) ------------
DO $$
DECLARE r record;
BEGIN
  FOR r IN
    SELECT p.oid::regprocedure AS sig
    FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
    WHERE n.nspname IN ('crm','billing')
    ORDER BY 1
  LOOP
    EXECUTE format('ALTER FUNCTION %s OWNER TO gk_crm_backend', r.sig);
  END LOOP;
END $$;

\echo '  ok: crm/billing functions re-owned to gk_crm_backend'

-- --- verify -----------------------------------------------------------------
\echo ''
\echo '-- table grants (sample) --'
SELECT grantee, table_schema, count(*) AS tables, string_agg(DISTINCT privilege_type, ',') AS privileges
FROM information_schema.role_table_grants
WHERE grantee LIKE 'gk_crm_%' AND table_schema IN ('crm','billing','audit')
GROUP BY grantee, table_schema
ORDER BY table_schema, grantee;

\echo ''
\echo '-- SECURITY DEFINER functions now owned by gk_crm_backend --'
SELECT n.nspname AS schema, count(*) AS definer_functions
FROM pg_proc p
JOIN pg_namespace n ON n.oid = p.pronamespace
JOIN pg_roles r ON r.oid = p.proowner
WHERE n.nspname IN ('crm','billing') AND p.prosecdef AND r.rolname = 'gk_crm_backend'
GROUP BY n.nspname ORDER BY n.nspname;

\echo ''
\echo 'ok: 0700 done.'
