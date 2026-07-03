-- ============================================================================
-- 0204_fix_audit_trigger_tenant_resolution.sql
-- Fix bug in audit.fn_capture_change() where:
--   (a) for tenancy.tenants itself, NEW has no tenant_id column → the trigger
--       fell back to the all-zeros UUID, violating fk_audit_events__tenants.
--   (b) for any audited table whose tenant could not be resolved, the trigger
--       still tried to insert with the bogus all-zeros tenant id.
--
-- The fixed function:
--   • Special-cases tenancy.tenants → uses NEW.id as the audit row's tenant_id.
--   • For everything else, resolves tenant_id from NEW.tenant_id then OLD.tenant_id.
--   • If neither is available, SKIPS the audit insert (no bogus FK) — the rest
--     of the transaction commits normally.
--
-- Idempotent (CREATE OR REPLACE).
-- ============================================================================

\echo '── 0204 patch: audit.fn_capture_change tenant resolution ──'

CREATE OR REPLACE FUNCTION audit.fn_capture_change()
RETURNS trigger
LANGUAGE plpgsql
AS $$
DECLARE
    v_tenant_id uuid;
    v_actor     uuid;
    v_label     text;
BEGIN
    -- ─── Resolve tenant_id ───────────────────────────────────────────────
    -- Special case: when auditing tenancy.tenants itself, NEW.id IS the tenant.
    IF TG_TABLE_SCHEMA = 'tenancy' AND TG_TABLE_NAME = 'tenants' THEN
        IF TG_OP IN ('INSERT','UPDATE') THEN
            BEGIN v_tenant_id := (row_to_json(NEW) ->> 'id')::uuid; EXCEPTION WHEN OTHERS THEN v_tenant_id := NULL; END;
        ELSIF TG_OP = 'DELETE' THEN
            BEGIN v_tenant_id := (row_to_json(OLD) ->> 'id')::uuid; EXCEPTION WHEN OTHERS THEN v_tenant_id := NULL; END;
        END IF;
    ELSE
        IF TG_OP IN ('INSERT','UPDATE') THEN
            BEGIN v_tenant_id := (row_to_json(NEW) ->> 'tenant_id')::uuid; EXCEPTION WHEN OTHERS THEN v_tenant_id := NULL; END;
        END IF;
        IF v_tenant_id IS NULL AND TG_OP IN ('UPDATE','DELETE') THEN
            BEGIN v_tenant_id := (row_to_json(OLD) ->> 'tenant_id')::uuid; EXCEPTION WHEN OTHERS THEN v_tenant_id := NULL; END;
        END IF;
    END IF;

    -- If we still can't determine a tenant, skip rather than violate the FK.
    -- (Use audit.write_audit_event(...) explicitly for system-wide events.)
    IF v_tenant_id IS NULL THEN
        RETURN COALESCE(NEW, OLD);
    END IF;

    -- ─── Resolve actor ───────────────────────────────────────────────────
    BEGIN v_actor := current_setting('app.actor_user_id', true)::uuid; EXCEPTION WHEN OTHERS THEN v_actor := NULL; END;
    v_label := COALESCE(NULLIF(current_setting('app.actor_label', true), ''), session_user);

    INSERT INTO audit.audit_events(
        tenant_id, actor_type, actor_user_id, actor_label,
        log_type, activity, module, sub_module,
        target_type, target_id, before_json, after_json, occurred_at
    ) VALUES (
        v_tenant_id,
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

\echo '✓ 0204 done.'
