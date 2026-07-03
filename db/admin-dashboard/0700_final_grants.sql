-- ============================================================================
-- 0700_final_grants.sql
-- Final pass: grant DML/SELECT on every existing table to the appropriate roles.
-- Default privileges (set in 0003) handle FUTURE tables; this file covers
-- everything that already exists at the time of running.
-- ============================================================================

\echo '── 0700 final grants ──'

DO $$
DECLARE
  schema_name text;
  schemas text[] := ARRAY[
    'tenancy','identity','kiosk','store','customer','tx','pricing',
    'payment','doc','ops','monitor','helpdesk','merchant','voucher',
    'audit','integration','config','compliance','reporting'
  ];
BEGIN
  FOREACH schema_name IN ARRAY schemas LOOP
    EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES    IN SCHEMA %I TO gk_app',          schema_name);
    EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES    IN SCHEMA %I TO gk_backend',      schema_name);
    EXECUTE format('GRANT SELECT                          ON ALL TABLES    IN SCHEMA %I TO gk_app_readonly', schema_name);
    EXECUTE format('GRANT USAGE, SELECT                   ON ALL SEQUENCES IN SCHEMA %I TO gk_app, gk_backend', schema_name);
    EXECUTE format('GRANT SELECT                          ON ALL SEQUENCES IN SCHEMA %I TO gk_app_readonly', schema_name);
    EXECUTE format('GRANT EXECUTE                         ON ALL FUNCTIONS IN SCHEMA %I TO gk_app, gk_backend', schema_name);
  END LOOP;

  -- Audit reader: audit + compliance only
  EXECUTE 'GRANT SELECT ON ALL TABLES IN SCHEMA audit      TO gk_audit_reader';
  EXECUTE 'GRANT SELECT ON ALL TABLES IN SCHEMA compliance TO gk_audit_reader';
END $$;

\echo '── verify (sample) ──'
SELECT grantee, table_schema, count(*) AS table_count, string_agg(DISTINCT privilege_type, ',') AS privileges
FROM information_schema.role_table_grants
WHERE grantee LIKE 'gk_%'
  AND table_schema IN ('tenancy','customer','tx','audit')
GROUP BY grantee, table_schema
ORDER BY table_schema, grantee;

\echo '✓ 0700 done.'
