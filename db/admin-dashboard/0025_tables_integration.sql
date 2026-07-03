-- ============================================================================
-- 0025_tables_integration.sql
-- Schema: integration
-- Tables: webhook_endpoints, webhook_subscriptions, webhook_deliveries,
--         webhook_delivery_attempts, integration_credentials
-- ============================================================================

\echo '── 0025 integration ──'

-- ─── webhook_endpoints ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS integration.webhook_endpoints (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id            uuid NOT NULL,
    url                  text NOT NULL,
    signing_key_kv_uri   text NOT NULL,
    status               text NOT NULL DEFAULT 'active',
    created_at           timestamptz NOT NULL DEFAULT now(),
    disabled_at          timestamptz NULL,
    CONSTRAINT fk_webhook_endpoints__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT ck_webhook_endpoints__status CHECK (status IN ('active','paused','disabled'))
);

-- ─── webhook_subscriptions ──────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS integration.webhook_subscriptions (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    webhook_endpoint_id    uuid NOT NULL,
    event_type             text NOT NULL,
    is_enabled             boolean NOT NULL DEFAULT true,
    CONSTRAINT fk_webhook_subscriptions__endpoints FOREIGN KEY (webhook_endpoint_id) REFERENCES integration.webhook_endpoints(id) ON DELETE CASCADE,
    CONSTRAINT uq_webhook_subscriptions__endpoint_event UNIQUE (webhook_endpoint_id, event_type)
);

-- ─── webhook_deliveries ─────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS integration.webhook_deliveries (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    webhook_endpoint_id   uuid NOT NULL,
    event_id              uuid NOT NULL,
    event_type            text NOT NULL,
    payload_json          jsonb NOT NULL,
    status                text NOT NULL DEFAULT 'pending',
    attempt_count         integer NOT NULL DEFAULT 0,
    last_error            text NULL,
    next_attempt_at       timestamptz NULL,
    created_at            timestamptz NOT NULL DEFAULT now(),
    completed_at          timestamptz NULL,
    CONSTRAINT fk_webhook_deliveries__endpoints FOREIGN KEY (webhook_endpoint_id) REFERENCES integration.webhook_endpoints(id) ON DELETE CASCADE,
    CONSTRAINT ck_webhook_deliveries__status CHECK (status IN ('pending','delivered','failed','dead'))
);

CREATE INDEX IF NOT EXISTS ix_webhook_deliveries__pending ON integration.webhook_deliveries(next_attempt_at) WHERE status = 'pending';

-- ─── webhook_delivery_attempts (append-only) ────────────────────────────────
CREATE TABLE IF NOT EXISTS integration.webhook_delivery_attempts (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    webhook_delivery_id    uuid NOT NULL,
    attempt_no             integer NOT NULL,
    request_hash           bytea NOT NULL,
    response_code          integer NULL,
    response_body_excerpt  text NULL,
    latency_ms             integer NULL,
    attempted_at           timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_wda__deliveries FOREIGN KEY (webhook_delivery_id) REFERENCES integration.webhook_deliveries(id) ON DELETE CASCADE,
    CONSTRAINT uq_wda__delivery_attempt UNIQUE (webhook_delivery_id, attempt_no)
);

-- ─── integration_credentials ────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS integration.integration_credentials (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id          uuid NOT NULL,
    provider           text NOT NULL,
    scope              text NOT NULL,
    kv_secret_uri      text NOT NULL,
    rotation_days      integer NOT NULL DEFAULT 90,
    last_rotated_at    timestamptz NULL,
    CONSTRAINT fk_integration_credentials__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_integration_credentials__tenant_provider_scope UNIQUE (tenant_id, provider, scope)
);

\echo '✓ 0025 integration done.'
