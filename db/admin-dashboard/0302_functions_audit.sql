-- ============================================================================
-- 0302_functions_audit.sql
-- Convenience helper for writing non-trigger-driven audit events (e.g. failed
-- sign-in attempts, manual admin actions outside of CRUD).
-- ============================================================================

\echo '── 0302 functions: audit helpers ──'

CREATE OR REPLACE FUNCTION audit.write_audit_event(
    p_tenant_id      uuid,
    p_activity       text,
    p_module         text DEFAULT NULL,
    p_sub_module     text DEFAULT NULL,
    p_target_type    text DEFAULT NULL,
    p_target_id      uuid DEFAULT NULL,
    p_before_json    jsonb DEFAULT NULL,
    p_after_json     jsonb DEFAULT NULL,
    p_log_type       text DEFAULT 'INFO',
    p_source_ip      inet DEFAULT NULL,
    p_correlation_id uuid DEFAULT NULL
)
RETURNS uuid
LANGUAGE plpgsql
AS $$
DECLARE
    v_id     uuid;
    v_actor  uuid;
    v_label  text;
BEGIN
    BEGIN v_actor := current_setting('app.actor_user_id', true)::uuid; EXCEPTION WHEN OTHERS THEN v_actor := NULL; END;
    v_label := COALESCE(current_setting('app.actor_label', true), session_user);

    INSERT INTO audit.audit_events(
        tenant_id, actor_type, actor_user_id, actor_label,
        log_type, activity, module, sub_module,
        target_type, target_id, before_json, after_json,
        source_ip, correlation_id, occurred_at
    ) VALUES (
        p_tenant_id,
        CASE WHEN v_actor IS NOT NULL THEN 'user' ELSE 'system' END,
        v_actor, v_label,
        p_log_type, p_activity, p_module, p_sub_module,
        p_target_type, p_target_id, p_before_json, p_after_json,
        p_source_ip, p_correlation_id, now()
    )
    RETURNING id INTO v_id;

    RETURN v_id;
END;
$$;

COMMENT ON FUNCTION audit.write_audit_event
  IS 'Manual audit event emitter. Used for events that don''t come from CRUD triggers (failed logins, manual actions, etc.).';

\echo '✓ 0302 done.'
