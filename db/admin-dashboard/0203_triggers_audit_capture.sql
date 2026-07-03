-- ============================================================================
-- 0203_triggers_audit_capture.sql
-- Attaches an audit trigger to business-meaningful tables. The trigger writes
-- a row to audit.audit_events with before/after snapshots (jsonb).
-- ============================================================================

\echo '── 0203 triggers: audit capture ──'

-- ─── generic audit-capture function ─────────────────────────────────────────
CREATE OR REPLACE FUNCTION audit.fn_capture_change()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_tenant_id uuid;
    v_actor     uuid;
    v_label     text;
BEGIN
    -- Best-effort tenant_id: prefer NEW, fall back to OLD, finally NULL.
    IF TG_OP IN ('INSERT','UPDATE') THEN
        BEGIN v_tenant_id := (row_to_json(NEW) ->> 'tenant_id')::uuid; EXCEPTION WHEN OTHERS THEN v_tenant_id := NULL; END;
    END IF;
    IF v_tenant_id IS NULL AND TG_OP IN ('UPDATE','DELETE') THEN
        BEGIN v_tenant_id := (row_to_json(OLD) ->> 'tenant_id')::uuid; EXCEPTION WHEN OTHERS THEN v_tenant_id := NULL; END;
    END IF;

    -- Resolve actor from GUCs (set by the app via set_tenant_context / set_actor_context).
    BEGIN v_actor := current_setting('app.actor_user_id', true)::uuid; EXCEPTION WHEN OTHERS THEN v_actor := NULL; END;
    v_label := COALESCE(current_setting('app.actor_label', true), session_user);

    INSERT INTO audit.audit_events(
        tenant_id, actor_type, actor_user_id, actor_label,
        log_type, activity, module, sub_module,
        target_type, target_id, before_json, after_json, occurred_at
    ) VALUES (
        COALESCE(v_tenant_id, '00000000-0000-0000-0000-000000000000'::uuid),
        CASE WHEN v_actor IS NOT NULL THEN 'user' ELSE 'system' END,
        v_actor,
        v_label,
        'INFO',
        format('%s.%s', TG_TABLE_SCHEMA, lower(TG_OP)),
        TG_TABLE_SCHEMA,
        TG_TABLE_NAME,
        TG_TABLE_NAME,
        CASE TG_OP WHEN 'DELETE' THEN (row_to_json(OLD) ->> 'id')::uuid
                   ELSE                (row_to_json(NEW) ->> 'id')::uuid END,
        CASE WHEN TG_OP IN ('UPDATE','DELETE') THEN to_jsonb(OLD) END,
        CASE WHEN TG_OP IN ('INSERT','UPDATE') THEN to_jsonb(NEW) END,
        now()
    );

    RETURN COALESCE(NEW, OLD);
END;
$$;

-- ─── attach to business-meaningful tables ────────────────────────────────────
DO $$
DECLARE
  tbl text;
  parts text[];
  trig_name text;
  audited_tables text[] := ARRAY[
    'tenancy.tenants',
    'tenancy.tenant_features',
    'identity.users',
    'identity.roles',
    'identity.user_roles',
    'kiosk.kiosks',
    'kiosk.devices',
    'kiosk.screen_savers',
    'store.stores',
    'customer.customers',
    'customer.customer_wallets',
    'customer.kyc_verifications',
    'tx.transactions',
    'pricing.pricing_policies',
    'pricing.offers',
    'payment.payments',
    'payment.payouts',
    'payment.refunds',
    'voucher.vouchers',
    'merchant.merchants',
    'merchant.franchise_wallets',
    'ops.deployment_tickets',
    'ops.maintenance_tickets',
    'helpdesk.support_tickets',
    'compliance.data_subject_requests'
  ];
BEGIN
  FOREACH tbl IN ARRAY audited_tables LOOP
    parts := string_to_array(tbl, '.');
    trig_name := format('tg_%I__aiud_audit', parts[2]);
    IF EXISTS (SELECT 1 FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
               WHERE n.nspname = parts[1] AND c.relname = parts[2]) THEN
      EXECUTE format('DROP TRIGGER IF EXISTS %I ON %I.%I', trig_name, parts[1], parts[2]);
      EXECUTE format('CREATE TRIGGER %I
                      AFTER INSERT OR UPDATE OR DELETE ON %I.%I
                      FOR EACH ROW EXECUTE FUNCTION audit.fn_capture_change()',
                      trig_name, parts[1], parts[2]);
      RAISE NOTICE '  ✓ audit on %', tbl;
    END IF;
  END LOOP;
END $$;

\echo '✓ 0203 done.'
