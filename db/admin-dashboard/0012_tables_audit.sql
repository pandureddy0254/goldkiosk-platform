-- ============================================================================
-- 0012_tables_audit.sql
-- Schema: audit
-- Append-only audit + outbox/inbox messaging.
-- Tables: audit_events, data_access_log, outbox_messages, inbox_messages
-- ============================================================================

\echo '── 0012 audit ──'

-- ─── audit_events (append-only) ─────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS audit.audit_events (
    sequence_no      bigint GENERATED ALWAYS AS IDENTITY,
    id               uuid NOT NULL DEFAULT gen_random_uuid(),
    tenant_id        uuid NOT NULL,
    actor_type       text NOT NULL,
    actor_user_id    uuid NULL,
    actor_label      text NULL,
    log_type         text NOT NULL DEFAULT 'INFO',
    activity         text NOT NULL,
    module           text NULL,
    sub_module       text NULL,
    sub_sub_module   text NULL,
    target_type      text NULL,
    target_id        uuid NULL,
    before_json      jsonb NULL,
    after_json       jsonb NULL,
    correlation_id   uuid NULL,
    source_ip        inet NULL,
    occurred_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_audit_events PRIMARY KEY (sequence_no),
    CONSTRAINT uq_audit_events__id UNIQUE (id),
    CONSTRAINT fk_audit_events__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id),
    CONSTRAINT ck_audit_events__actor_type CHECK (actor_type IN ('user','kiosk','system','service')),
    CONSTRAINT ck_audit_events__log_type   CHECK (log_type   IN ('INFO','WARN','ERROR','SECURITY'))
);

CREATE INDEX IF NOT EXISTS ix_audit_events__tenant_occurred  ON audit.audit_events(tenant_id, occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_audit_events__activity         ON audit.audit_events(activity);
CREATE INDEX IF NOT EXISTS ix_audit_events__target           ON audit.audit_events(target_type, target_id) WHERE target_id IS NOT NULL;

-- ─── data_access_log (append-only) ──────────────────────────────────────────
CREATE TABLE IF NOT EXISTS audit.data_access_log (
    sequence_no      bigint GENERATED ALWAYS AS IDENTITY,
    tenant_id        uuid NOT NULL,
    actor_user_id    uuid NULL,
    actor_label      text NOT NULL,
    target_type      text NOT NULL,
    target_id        uuid NOT NULL,
    field_name       text NOT NULL,
    access_reason    text NULL,
    correlation_id   uuid NULL,
    occurred_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_data_access_log PRIMARY KEY (sequence_no),
    CONSTRAINT fk_data_access_log__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id)
);

CREATE INDEX IF NOT EXISTS ix_data_access_log__tenant_occurred ON audit.data_access_log(tenant_id, occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_data_access_log__target          ON audit.data_access_log(target_type, target_id);

-- ─── outbox_messages ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS audit.outbox_messages (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL,
    aggregate_type  text NOT NULL,
    aggregate_id    uuid NOT NULL,
    message_type    text NOT NULL,
    payload_json    jsonb NOT NULL,
    status          text NOT NULL DEFAULT 'pending',
    occurred_at     timestamptz NOT NULL DEFAULT now(),
    published_at    timestamptz NULL,
    attempts        integer NOT NULL DEFAULT 0,
    last_error      text NULL,
    CONSTRAINT fk_outbox_messages__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id),
    CONSTRAINT ck_outbox_messages__status CHECK (status IN ('pending','published','failed'))
);

CREATE INDEX IF NOT EXISTS ix_outbox_messages__pending ON audit.outbox_messages(occurred_at) WHERE status = 'pending';
CREATE INDEX IF NOT EXISTS ix_outbox_messages__aggregate ON audit.outbox_messages(aggregate_type, aggregate_id);

-- ─── inbox_messages ─────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS audit.inbox_messages (
    source         text NOT NULL,
    message_id     text NOT NULL,
    message_type   text NOT NULL,
    received_at    timestamptz NOT NULL DEFAULT now(),
    processed_at   timestamptz NULL,
    CONSTRAINT pk_inbox_messages PRIMARY KEY (source, message_id)
);

CREATE INDEX IF NOT EXISTS ix_inbox_messages__unprocessed ON audit.inbox_messages(received_at) WHERE processed_at IS NULL;

\echo '✓ 0012 audit done.'
