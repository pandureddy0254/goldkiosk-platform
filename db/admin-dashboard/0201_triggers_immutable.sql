-- ============================================================================
-- 0201_triggers_immutable.sql
-- Attach public.fn_reject_modify() to append-only tables.
-- These tables accept INSERTs only; UPDATE/DELETE raise an exception.
-- ============================================================================

\echo '── 0201 triggers: append-only enforcement ──'

DO $$
DECLARE
  tbl text;
  parts text[];
  trig_name text;
  immutable_tables text[] := ARRAY[
    'audit.audit_events',
    'audit.data_access_log',
    'customer.wallet_ledger_entries',
    'merchant.franchise_wallet_ledger',
    'tx.transaction_events',
    'tx.weight_readings',
    'kiosk.kiosk_heartbeats',
    'kiosk.device_fault_logs',
    'monitor.api_request_logs',
    'payment.payout_attempts',
    'integration.webhook_delivery_attempts',
    'pricing.fx_rate_snapshots',
    'pricing.market_snapshots',
    'ops.kiosk_inventory_snapshots',
    'ops.system_health_snapshots',
    'monitor.api_health_checks'
  ];
BEGIN
  FOREACH tbl IN ARRAY immutable_tables LOOP
    parts := string_to_array(tbl, '.');
    trig_name := format('tg_%I__bud_immutable', parts[2]);
    -- Skip if the table doesn't exist (allows partial environments).
    IF EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
               WHERE n.nspname = parts[1] AND c.relname = parts[2]) THEN
      EXECUTE format('DROP TRIGGER IF EXISTS %I ON %I.%I', trig_name, parts[1], parts[2]);
      EXECUTE format('CREATE TRIGGER %I BEFORE UPDATE OR DELETE ON %I.%I
                      FOR EACH ROW EXECUTE FUNCTION public.fn_reject_modify()',
                      trig_name, parts[1], parts[2]);
      RAISE NOTICE '  ✓ %', tbl;
    ELSE
      RAISE NOTICE '  ⤬ % (table not present)', tbl;
    END IF;
  END LOOP;
END $$;

\echo '✓ 0201 done.'
