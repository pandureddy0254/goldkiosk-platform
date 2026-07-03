-- ============================================================================
-- 0021_tables_monitor.sql
-- Schema: monitor
-- Tables: exception_logs, api_request_logs (partitioned), api_health_checks
-- ============================================================================

\echo '── 0021 monitor ──'

-- ─── exception_logs ─────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS monitor.exception_logs (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id            uuid NULL,
    kiosk_id             uuid NULL,
    user_id              uuid NULL,
    source               text NOT NULL,
    exception_name       text NOT NULL,
    message              text NOT NULL,
    stack_trace          text NULL,
    severity             text NOT NULL DEFAULT 'error',
    occurred_at          timestamptz NOT NULL DEFAULT now(),
    is_resolved          boolean NOT NULL DEFAULT false,
    resolved_by_user_id  uuid NULL,
    resolved_at          timestamptz NULL,
    correlation_id       uuid NULL,
    CONSTRAINT fk_exception_logs__tenants  FOREIGN KEY (tenant_id)           REFERENCES tenancy.tenants(id),
    CONSTRAINT fk_exception_logs__kiosks   FOREIGN KEY (kiosk_id)            REFERENCES kiosk.kiosks(id),
    CONSTRAINT fk_exception_logs__users    FOREIGN KEY (user_id)             REFERENCES identity.users(id),
    CONSTRAINT fk_exception_logs__resolver FOREIGN KEY (resolved_by_user_id) REFERENCES identity.users(id),
    CONSTRAINT ck_exception_logs__source   CHECK (source   IN ('kiosk','admin','api','job','integration')),
    CONSTRAINT ck_exception_logs__severity CHECK (severity IN ('info','warn','error','critical'))
);

CREATE INDEX IF NOT EXISTS ix_exception_logs__unresolved ON monitor.exception_logs(occurred_at DESC) WHERE is_resolved = false;
CREATE INDEX IF NOT EXISTS ix_exception_logs__tenant_time ON monitor.exception_logs(tenant_id, occurred_at DESC);

-- ─── api_request_logs (partitioned monthly) ─────────────────────────────────
CREATE TABLE IF NOT EXISTS monitor.api_request_logs (
    sequence_no       bigint GENERATED ALWAYS AS IDENTITY,
    tenant_id         uuid NULL,
    endpoint          text NOT NULL,
    requested_at      timestamptz NOT NULL DEFAULT now(),
    response_time_ms  integer NOT NULL,
    status_code       integer NOT NULL,
    is_success        boolean NOT NULL,
    caller_kiosk_id   uuid NULL,
    caller_user_id    uuid NULL,
    correlation_id    uuid NULL,
    CONSTRAINT pk_api_request_logs PRIMARY KEY (sequence_no, requested_at)
) PARTITION BY RANGE (requested_at);

CREATE TABLE IF NOT EXISTS monitor.api_request_logs_default PARTITION OF monitor.api_request_logs DEFAULT;

CREATE INDEX IF NOT EXISTS ix_api_request_logs__endpoint_time ON monitor.api_request_logs(endpoint, requested_at DESC);
CREATE INDEX IF NOT EXISTS ix_api_request_logs__tenant ON monitor.api_request_logs(tenant_id, requested_at DESC) WHERE tenant_id IS NOT NULL;

-- ─── api_health_checks ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS monitor.api_health_checks (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    endpoint          text NOT NULL,
    checked_at        timestamptz NOT NULL DEFAULT now(),
    response_time_ms  integer NOT NULL,
    status_code       integer NOT NULL,
    is_healthy        boolean NOT NULL,
    region            text NULL
);

CREATE INDEX IF NOT EXISTS ix_api_health_checks__endpoint_time ON monitor.api_health_checks(endpoint, checked_at DESC);

\echo '✓ 0021 monitor done.'
