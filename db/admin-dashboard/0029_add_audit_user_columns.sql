-- ============================================================================
-- 0029_add_audit_user_columns.sql
-- Ensures every table that already has `created_at`/`updated_at` also has
-- `created_by_user_id` and `updated_by_user_id` (the columns the EF Core
-- IAuditableEntity interface expects). Idempotent: ADD COLUMN IF NOT EXISTS.
-- Skips identity.users itself (which doesn't track its own author).
-- ============================================================================

\echo '── 0029 add audit-user columns to timestamped tables ──'

DO $$
DECLARE
    r record;
    sql text;
BEGIN
    FOR r IN
        SELECT n.nspname AS schema_name, c.relname AS table_name
        FROM pg_class c
        JOIN pg_namespace n ON n.oid = c.relnamespace
        JOIN pg_attribute a ON a.attrelid = c.oid
        WHERE c.relkind = 'r'
          AND a.attname = 'created_at'
          AND a.attnum > 0
          AND NOT a.attisdropped
          AND n.nspname NOT IN ('pg_catalog', 'information_schema', 'public')
          -- skip identity.users (managed via Identity columns, no self-author)
          AND NOT (n.nspname = 'identity' AND c.relname = 'users')
        ORDER BY 1, 2
    LOOP
        sql := format(
            'ALTER TABLE %I.%I
                ADD COLUMN IF NOT EXISTS created_by_user_id uuid NULL,
                ADD COLUMN IF NOT EXISTS updated_by_user_id uuid NULL',
            r.schema_name, r.table_name);
        EXECUTE sql;
    END LOOP;
END $$;

\echo '✓ 0029 done.'
