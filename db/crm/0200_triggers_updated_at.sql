-- ============================================================================
-- 0200_triggers_updated_at.sql
-- Attach public.fn_set_updated_at() as a BEFORE UPDATE trigger to every table
-- in crm/billing/audit that has an `updated_at` column.
-- Idempotent (drops + recreates).
-- ============================================================================

\echo '-- 0200 triggers: set_updated_at --'

DO $$
DECLARE
  r record;
  trig_name text;
BEGIN
  FOR r IN
    SELECT n.nspname AS schema_name, c.relname AS table_name
    FROM pg_class c
    JOIN pg_namespace n ON n.oid = c.relnamespace
    JOIN pg_attribute a ON a.attrelid = c.oid
    WHERE c.relkind = 'r'
      AND a.attname = 'updated_at'
      AND a.attnum > 0
      AND NOT a.attisdropped
      AND n.nspname IN ('crm','billing','audit')
    ORDER BY 1, 2
  LOOP
    trig_name := format('tg_%s__bu_set_updated_at', r.table_name);
    EXECUTE format('DROP TRIGGER IF EXISTS %I ON %I.%I', trig_name, r.schema_name, r.table_name);
    EXECUTE format('CREATE TRIGGER %I BEFORE UPDATE ON %I.%I
                    FOR EACH ROW EXECUTE FUNCTION public.fn_set_updated_at()',
                    trig_name, r.schema_name, r.table_name);
    RAISE NOTICE '  ok: %.%', r.schema_name, r.table_name;
  END LOOP;
END $$;

\echo 'ok: 0200 done.'
