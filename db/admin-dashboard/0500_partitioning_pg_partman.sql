-- ============================================================================
-- 0500_partitioning_pg_partman.sql
-- Set up pg_partman to manage partition lifecycle on the four heavily-written
-- partitioned tables (kiosk.kiosk_heartbeats, monitor.api_request_logs,
-- audit.audit_events*, tx.transaction_events).
--
-- * audit.audit_events isn't partitioned in the initial DDL (0012) — it remains
--   a single table for now; conversion to RANGE partitioning is left as a
--   follow-up because converting a populated table to a partitioned one in PG
--   requires a swap-and-rename and isn't appropriate for `IF NOT EXISTS` idempotent
--   bootstrapping.
--
-- NOTE: pg_partman requires the extension to be allowlisted on the server.
-- On Azure Flexible Server: set `azure.extensions = PG_PARTMAN,PG_PARTMAN_BGW,...`
-- in server parameters BEFORE running this file.
-- ============================================================================

\echo '── 0500 partitioning ──'

-- Try to enable pg_partman. Skip the rest of this file if it isn't available.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_available_extensions WHERE name = 'pg_partman') THEN
        CREATE EXTENSION IF NOT EXISTS pg_partman SCHEMA partman;
    ELSE
        RAISE NOTICE 'pg_partman not available on this server — skipping pg_partman setup. Default partitions will accept all inserts; create monthly/weekly partitions manually.';
    END IF;
EXCEPTION WHEN OTHERS THEN
    RAISE NOTICE 'pg_partman extension setup skipped: %', SQLERRM;
END $$;

-- The actual `partman.create_parent(...)` calls run only when the extension exists.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pg_partman') THEN
        -- kiosk_heartbeats: weekly partitions, retain 30 days
        BEGIN
            PERFORM partman.create_parent(
                p_parent_table => 'kiosk.kiosk_heartbeats',
                p_control      => 'at',
                p_type         => 'range',
                p_interval     => '1 week'
            );
            UPDATE partman.part_config SET retention = '30 days', retention_keep_table = false
             WHERE parent_table = 'kiosk.kiosk_heartbeats';
        EXCEPTION WHEN OTHERS THEN RAISE NOTICE 'kiosk_heartbeats partition setup: %', SQLERRM; END;

        -- api_request_logs: monthly partitions, retain 90 days
        BEGIN
            PERFORM partman.create_parent(
                p_parent_table => 'monitor.api_request_logs',
                p_control      => 'requested_at',
                p_type         => 'range',
                p_interval     => '1 month'
            );
            UPDATE partman.part_config SET retention = '90 days', retention_keep_table = false
             WHERE parent_table = 'monitor.api_request_logs';
        EXCEPTION WHEN OTHERS THEN RAISE NOTICE 'api_request_logs partition setup: %', SQLERRM; END;

        -- transaction_events: monthly partitions, retain 7 years (legal)
        BEGIN
            PERFORM partman.create_parent(
                p_parent_table => 'tx.transaction_events',
                p_control      => 'occurred_at',
                p_type         => 'range',
                p_interval     => '1 month'
            );
            UPDATE partman.part_config SET retention = '7 years', retention_keep_table = true
             WHERE parent_table = 'tx.transaction_events';
        EXCEPTION WHEN OTHERS THEN RAISE NOTICE 'transaction_events partition setup: %', SQLERRM; END;
    END IF;
END $$;

\echo '✓ 0500 done.'
