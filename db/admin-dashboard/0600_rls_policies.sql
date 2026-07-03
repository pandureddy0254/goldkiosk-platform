-- ============================================================================
-- 0600_rls_policies.sql
-- Enable Row-Level Security on every tenant-scoped table and attach a uniform
-- isolation policy. The policy reads `app.tenant_id` GUC, which the app sets
-- per request via tenancy.set_tenant_context().
--
-- gk_backend bypasses RLS (created with BYPASSRLS in 0003).
-- ============================================================================

\echo '── 0600 RLS policies ──'

DO $$
DECLARE
  r record;
  schemas_to_rls text[] := ARRAY[
    'tenancy','identity','kiosk','store','customer','tx','pricing',
    'payment','doc','ops','monitor','helpdesk','merchant','voucher',
    'audit','integration','config','compliance'
  ];
  schema_name text;
BEGIN
  FOREACH schema_name IN ARRAY schemas_to_rls LOOP
    FOR r IN
      SELECT c.relname AS table_name
      FROM pg_class c
      JOIN pg_namespace n ON n.oid = c.relnamespace
      JOIN pg_attribute a ON a.attrelid = c.oid
      WHERE c.relkind = 'r'                  -- ordinary tables only (not partitions)
        AND a.attname = 'tenant_id'
        AND a.attnum > 0
        AND NOT a.attisdropped
        AND n.nspname = schema_name
        AND NOT c.relispartition              -- skip partition children; policy inherits from parent
    LOOP
      EXECUTE format('ALTER TABLE %I.%I ENABLE ROW LEVEL SECURITY', schema_name, r.table_name);
      EXECUTE format('ALTER TABLE %I.%I FORCE ROW LEVEL SECURITY',  schema_name, r.table_name);

      EXECUTE format('DROP POLICY IF EXISTS tenant_isolation ON %I.%I',
                     schema_name, r.table_name);
      EXECUTE format('CREATE POLICY tenant_isolation ON %I.%I
                      USING      (tenant_id = current_setting(''app.tenant_id'', true)::uuid)
                      WITH CHECK (tenant_id = current_setting(''app.tenant_id'', true)::uuid)',
                     schema_name, r.table_name);
      RAISE NOTICE '  ✓ %.%', schema_name, r.table_name;
    END LOOP;
  END LOOP;
END $$;

\echo '── verify ──'
SELECT schemaname, tablename, policyname FROM pg_policies
WHERE schemaname IN ('tenancy','identity','kiosk','store','customer','tx','pricing',
                     'payment','doc','ops','monitor','helpdesk','merchant','voucher',
                     'audit','integration','config','compliance')
ORDER BY schemaname, tablename;

\echo '✓ 0600 done.'
