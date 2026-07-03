-- ============================================================================
-- 0026_tables_config.sql
-- Schema: config
-- Tables: feature_flags, feature_flag_overrides, app_configs, schedules
-- ============================================================================

\echo '── 0026 config ──'

-- ─── feature_flags ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS config.feature_flags (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code           citext NOT NULL,
    display_name   text NOT NULL,
    description    text NULL,
    default_value  text NOT NULL,
    CONSTRAINT uq_feature_flags__code UNIQUE (code)
);

-- ─── feature_flag_overrides ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS config.feature_flag_overrides (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    feature_flag_id  uuid NOT NULL,
    tenant_id        uuid NULL,
    store_id         uuid NULL,
    kiosk_id         uuid NULL,
    user_id          uuid NULL,
    value            text NOT NULL,
    expires_at       timestamptz NULL,
    CONSTRAINT fk_ffo__flags    FOREIGN KEY (feature_flag_id) REFERENCES config.feature_flags(id) ON DELETE CASCADE,
    CONSTRAINT fk_ffo__tenants  FOREIGN KEY (tenant_id)       REFERENCES tenancy.tenants(id),
    CONSTRAINT fk_ffo__stores   FOREIGN KEY (store_id)        REFERENCES store.stores(id),
    CONSTRAINT fk_ffo__kiosks   FOREIGN KEY (kiosk_id)        REFERENCES kiosk.kiosks(id),
    CONSTRAINT fk_ffo__users    FOREIGN KEY (user_id)         REFERENCES identity.users(id)
);

CREATE INDEX IF NOT EXISTS ix_feature_flag_overrides__flag ON config.feature_flag_overrides(feature_flag_id);

-- ─── app_configs ────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS config.app_configs (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           uuid NULL,
    key                 text NOT NULL,
    value               text NOT NULL,
    value_type          text NOT NULL DEFAULT 'string',
    updated_at          timestamptz NOT NULL DEFAULT now(),
    updated_by_user_id  uuid NULL,
    CONSTRAINT fk_app_configs__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_app_configs__users   FOREIGN KEY (updated_by_user_id) REFERENCES identity.users(id),
    CONSTRAINT uq_app_configs__tenant_key UNIQUE (tenant_id, key),
    CONSTRAINT ck_app_configs__value_type CHECK (value_type IN ('string','int','bool','json'))
);

-- ─── schedules ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS config.schedules (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id        uuid NULL,
    job_code         citext NOT NULL,
    cron_expression  text NOT NULL,
    time_zone        text NOT NULL DEFAULT 'UTC',
    is_enabled       boolean NOT NULL DEFAULT true,
    last_run_at      timestamptz NULL,
    next_run_at      timestamptz NULL,
    CONSTRAINT fk_schedules__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_schedules__tenant_job UNIQUE (tenant_id, job_code)
);

CREATE INDEX IF NOT EXISTS ix_schedules__next_run ON config.schedules(next_run_at) WHERE is_enabled = true;

\echo '✓ 0026 config done.'
