-- ============================================================================
-- 0304_functions_compliance.sql
-- Retention sweep — walks compliance.retention_policies and pseudonymises /
-- deletes rows that have aged out. Called nightly by pg_cron or an external job.
-- ============================================================================

\echo '── 0304 functions: compliance ──'

CREATE OR REPLACE FUNCTION compliance.purge_expired_data(p_dry_run boolean DEFAULT true)
RETURNS jsonb
LANGUAGE plpgsql
AS $$
DECLARE
    v_summary  jsonb := '[]'::jsonb;
    v_policy   compliance.retention_policies%ROWTYPE;
    v_count    integer;
BEGIN
    FOR v_policy IN SELECT * FROM compliance.retention_policies WHERE is_active LOOP
        v_count := 0;

        IF v_policy.data_class = 'audit_events' THEN
            IF NOT p_dry_run THEN
                DELETE FROM audit.audit_events
                 WHERE tenant_id   = v_policy.tenant_id
                   AND occurred_at < now() - make_interval(days => v_policy.retention_days);
                GET DIAGNOSTICS v_count = ROW_COUNT;
            ELSE
                SELECT count(*) INTO v_count
                  FROM audit.audit_events
                 WHERE tenant_id   = v_policy.tenant_id
                   AND occurred_at < now() - make_interval(days => v_policy.retention_days);
            END IF;
        ELSIF v_policy.data_class = 'api_request_logs' THEN
            IF NOT p_dry_run THEN
                DELETE FROM monitor.api_request_logs
                 WHERE tenant_id    = v_policy.tenant_id
                   AND requested_at < now() - make_interval(days => v_policy.retention_days);
                GET DIAGNOSTICS v_count = ROW_COUNT;
            END IF;
        ELSIF v_policy.data_class = 'kiosk_heartbeats' THEN
            IF NOT p_dry_run THEN
                -- Heartbeats aren't tenant-scoped; this policy must be a tenant-wide one.
                DELETE FROM kiosk.kiosk_heartbeats
                 WHERE at < now() - make_interval(days => v_policy.retention_days);
                GET DIAGNOSTICS v_count = ROW_COUNT;
            END IF;
        ELSIF v_policy.data_class = 'item_photographs' THEN
            -- Real implementation would also delete the underlying blob; left as a placeholder.
            IF NOT p_dry_run THEN
                DELETE FROM tx.item_photographs
                 WHERE captured_at < now() - make_interval(days => v_policy.retention_days);
                GET DIAGNOSTICS v_count = ROW_COUNT;
            END IF;
        END IF;

        v_summary := v_summary || jsonb_build_object(
            'data_class',     v_policy.data_class,
            'tenant_id',      v_policy.tenant_id,
            'retention_days', v_policy.retention_days,
            'row_count',      v_count,
            'dry_run',        p_dry_run
        );
    END LOOP;

    RETURN v_summary;
END;
$$;

COMMENT ON FUNCTION compliance.purge_expired_data(boolean)
  IS 'Walks active retention_policies and deletes data older than retention_days. Pass false to actually delete; default is dry-run.';

\echo '✓ 0304 done.'
