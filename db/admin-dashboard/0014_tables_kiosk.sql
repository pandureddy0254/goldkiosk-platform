-- ============================================================================
-- 0014_tables_kiosk.sql
-- Schema: kiosk
-- Tables: kiosk_locations, kiosks, kiosk_registrations, kiosk_health,
--         kiosk_heartbeats, kiosk_security_tokens, device_models, devices,
--         device_calibrations, device_fault_logs, screen_savers,
--         kiosk_screen_saver_assignments
-- ============================================================================

\echo '── 0014 kiosk ──'

-- ─── kiosk_locations ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS kiosk.kiosk_locations (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   uuid NOT NULL,
    store_id    uuid NULL,
    code        citext NOT NULL,
    name        text NOT NULL,
    city        text NOT NULL,
    address     text NULL,
    latitude    numeric(9,6) NULL,
    longitude   numeric(9,6) NULL,
    region      text NULL,
    is_active   boolean NOT NULL DEFAULT true,
    CONSTRAINT fk_kiosk_locations__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_kiosk_locations__stores  FOREIGN KEY (store_id)  REFERENCES store.stores(id) ON DELETE SET NULL,
    CONSTRAINT uq_kiosk_locations__tenant_code UNIQUE (tenant_id, code)
);

-- ─── kiosks ─────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS kiosk.kiosks (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           uuid NOT NULL,
    store_id            uuid NOT NULL,
    location_id         uuid NULL,
    code                citext NOT NULL,
    friendly_name       text NOT NULL,
    hardware_model      text NOT NULL DEFAULT 'GK-CUBE-V3',
    device_id           text NULL,
    msix_channel        text NOT NULL DEFAULT 'ring1',
    app_version         text NULL,
    os_version          text NULL,
    cert_thumbprint     text NULL,
    cert_issued_at      timestamptz NULL,
    cert_expires_at     timestamptz NULL,
    pin_hash            text NULL,
    network_speed       text NULL,
    status              text NOT NULL DEFAULT 'onboarding',
    is_maintenance      boolean NOT NULL DEFAULT false,
    is_active           boolean NOT NULL DEFAULT true,
    last_ping_at        timestamptz NULL,
    onboarded_at        timestamptz NULL,
    decommissioned_at   timestamptz NULL,
    created_at          timestamptz NOT NULL DEFAULT now(),
    updated_at          timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_kiosks__tenants   FOREIGN KEY (tenant_id)   REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_kiosks__stores    FOREIGN KEY (store_id)    REFERENCES store.stores(id),
    CONSTRAINT fk_kiosks__locations  FOREIGN KEY (location_id) REFERENCES kiosk.kiosk_locations(id),
    CONSTRAINT uq_kiosks__tenant_code UNIQUE (tenant_id, code),
    CONSTRAINT ck_kiosks__msix_channel CHECK (msix_channel IN ('ring0','ring1','ring2')),
    CONSTRAINT ck_kiosks__status CHECK (status IN ('onboarding','active','maintenance','offline','decommissioned'))
);

CREATE INDEX IF NOT EXISTS ix_kiosks__tenant_status ON kiosk.kiosks(tenant_id, status) WHERE is_active = true;
CREATE INDEX IF NOT EXISTS ix_kiosks__store ON kiosk.kiosks(store_id);

-- ─── kiosk_registrations ────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS kiosk.kiosk_registrations (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    kiosk_id               uuid NOT NULL,
    bootstrap_token_hash   bytea NOT NULL,
    enrolled_at            timestamptz NOT NULL DEFAULT now(),
    enrolled_by_user_id    uuid NULL,
    cert_issued_at         timestamptz NOT NULL DEFAULT now(),
    last_cert_rotation_at  timestamptz NULL,
    CONSTRAINT fk_kiosk_registrations__kiosks FOREIGN KEY (kiosk_id) REFERENCES kiosk.kiosks(id) ON DELETE CASCADE,
    CONSTRAINT uq_kiosk_registrations__kiosk UNIQUE (kiosk_id),
    CONSTRAINT fk_kiosk_registrations__users FOREIGN KEY (enrolled_by_user_id) REFERENCES identity.users(id)
);

-- ─── kiosk_health (current snapshot, 1:1) ───────────────────────────────────
CREATE TABLE IF NOT EXISTS kiosk.kiosk_health (
    kiosk_id              uuid PRIMARY KEY,
    status                text NOT NULL DEFAULT 'green',
    last_heartbeat_at     timestamptz NOT NULL DEFAULT now(),
    last_transaction_at   timestamptz NULL,
    error_backlog_count   integer NOT NULL DEFAULT 0,
    disk_free_pct         numeric(5,2) NULL,
    cpu_pct               numeric(5,2) NULL,
    ram_pct               numeric(5,2) NULL,
    CONSTRAINT fk_kiosk_health__kiosks FOREIGN KEY (kiosk_id) REFERENCES kiosk.kiosks(id) ON DELETE CASCADE,
    CONSTRAINT ck_kiosk_health__status CHECK (status IN ('green','amber','red'))
);

-- ─── kiosk_heartbeats (append-only, partitioned by month) ───────────────────
CREATE TABLE IF NOT EXISTS kiosk.kiosk_heartbeats (
    sequence_no    bigint GENERATED ALWAYS AS IDENTITY,
    kiosk_id       uuid NOT NULL,
    at             timestamptz NOT NULL DEFAULT now(),
    cpu_pct        numeric(5,2) NULL,
    ram_pct        numeric(5,2) NULL,
    disk_free_pct  numeric(5,2) NULL,
    network_ms     integer NULL,
    payload_json   jsonb NULL,
    CONSTRAINT pk_kiosk_heartbeats PRIMARY KEY (sequence_no, at)
) PARTITION BY RANGE (at);

-- Default partition so inserts succeed before pg_partman is set up.
CREATE TABLE IF NOT EXISTS kiosk.kiosk_heartbeats_default PARTITION OF kiosk.kiosk_heartbeats DEFAULT;

CREATE INDEX IF NOT EXISTS ix_kiosk_heartbeats__kiosk_at ON kiosk.kiosk_heartbeats(kiosk_id, at DESC);

-- ─── kiosk_security_tokens ──────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS kiosk.kiosk_security_tokens (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    kiosk_id            uuid NOT NULL,
    token_hash          bytea NOT NULL,
    issued_at           timestamptz NOT NULL DEFAULT now(),
    expires_at          timestamptz NOT NULL,
    revoked_at          timestamptz NULL,
    last_used_at        timestamptz NULL,
    issued_by_user_id   uuid NULL,
    CONSTRAINT fk_kiosk_security_tokens__kiosks FOREIGN KEY (kiosk_id) REFERENCES kiosk.kiosks(id) ON DELETE CASCADE,
    CONSTRAINT fk_kiosk_security_tokens__users  FOREIGN KEY (issued_by_user_id) REFERENCES identity.users(id),
    CONSTRAINT uq_kiosk_security_tokens__hash UNIQUE (token_hash)
);

CREATE INDEX IF NOT EXISTS ix_kiosk_security_tokens__active ON kiosk.kiosk_security_tokens(kiosk_id) WHERE revoked_at IS NULL;

-- ─── device_models ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS kiosk.device_models (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    type                text NOT NULL,
    manufacturer        text NOT NULL,
    model_code          text NOT NULL,
    driver_name         text NOT NULL,
    capability_profile  jsonb NULL,
    min_firmware        text NULL,
    max_firmware        text NULL,
    CONSTRAINT uq_device_models__model UNIQUE (manufacturer, model_code),
    CONSTRAINT ck_device_models__type CHECK (type IN ('camera','scale','xrf','signature_pad','cash_dispenser','doc_scanner','fingerprint','printer'))
);

-- ─── devices ────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS kiosk.devices (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    kiosk_id          uuid NOT NULL,
    device_model_id   uuid NOT NULL,
    serial_number     text NOT NULL,
    firmware_version  text NULL,
    status            text NOT NULL DEFAULT 'active',
    installed_at      timestamptz NOT NULL DEFAULT now(),
    removed_at        timestamptz NULL,
    CONSTRAINT fk_devices__kiosks       FOREIGN KEY (kiosk_id)        REFERENCES kiosk.kiosks(id) ON DELETE CASCADE,
    CONSTRAINT fk_devices__device_models FOREIGN KEY (device_model_id) REFERENCES kiosk.device_models(id),
    CONSTRAINT uq_devices__serial UNIQUE (serial_number),
    CONSTRAINT ck_devices__status CHECK (status IN ('active','fault','removed'))
);

CREATE INDEX IF NOT EXISTS ix_devices__kiosk ON kiosk.devices(kiosk_id) WHERE removed_at IS NULL;

-- ─── device_calibrations ────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS kiosk.device_calibrations (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id             uuid NOT NULL,
    calibrated_at         timestamptz NOT NULL DEFAULT now(),
    operator_user_id      uuid NULL,
    before_readings_json  jsonb NULL,
    after_readings_json   jsonb NULL,
    certificate_id        text NULL,
    next_due_at           timestamptz NOT NULL,
    CONSTRAINT fk_device_calibrations__devices FOREIGN KEY (device_id) REFERENCES kiosk.devices(id) ON DELETE CASCADE,
    CONSTRAINT fk_device_calibrations__users   FOREIGN KEY (operator_user_id) REFERENCES identity.users(id)
);

CREATE INDEX IF NOT EXISTS ix_device_calibrations__device ON kiosk.device_calibrations(device_id, calibrated_at DESC);

-- ─── device_fault_logs (append-only) ────────────────────────────────────────
CREATE TABLE IF NOT EXISTS kiosk.device_fault_logs (
    sequence_no    bigint GENERATED ALWAYS AS IDENTITY,
    device_id      uuid NOT NULL,
    at             timestamptz NOT NULL DEFAULT now(),
    fault_code     text NOT NULL,
    severity       text NOT NULL DEFAULT 'warn',
    details_json   jsonb NULL,
    resolved_at    timestamptz NULL,
    CONSTRAINT pk_device_fault_logs PRIMARY KEY (sequence_no),
    CONSTRAINT fk_device_fault_logs__devices FOREIGN KEY (device_id) REFERENCES kiosk.devices(id) ON DELETE CASCADE,
    CONSTRAINT ck_device_fault_logs__severity CHECK (severity IN ('info','warn','error','critical'))
);

CREATE INDEX IF NOT EXISTS ix_device_fault_logs__device_at ON kiosk.device_fault_logs(device_id, at DESC);

-- ─── screen_savers ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS kiosk.screen_savers (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id         uuid NOT NULL,
    code              citext NOT NULL,
    image_url         text NOT NULL,
    media_type        text NOT NULL DEFAULT 'image',
    duration_seconds  integer NULL,
    display_order     integer NOT NULL DEFAULT 0,
    is_active         boolean NOT NULL DEFAULT true,
    created_at        timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_screen_savers__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_screen_savers__tenant_code UNIQUE (tenant_id, code),
    CONSTRAINT ck_screen_savers__media_type CHECK (media_type IN ('image','video'))
);

-- ─── kiosk_screen_saver_assignments ─────────────────────────────────────────
CREATE TABLE IF NOT EXISTS kiosk.kiosk_screen_saver_assignments (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    kiosk_id         uuid NOT NULL,
    screen_saver_id  uuid NOT NULL,
    sequence         integer NOT NULL DEFAULT 0,
    assigned_at      timestamptz NOT NULL DEFAULT now(),
    removed_at       timestamptz NULL,
    CONSTRAINT fk_kssa__kiosks        FOREIGN KEY (kiosk_id)        REFERENCES kiosk.kiosks(id) ON DELETE CASCADE,
    CONSTRAINT fk_kssa__screen_savers FOREIGN KEY (screen_saver_id) REFERENCES kiosk.screen_savers(id) ON DELETE CASCADE,
    CONSTRAINT uq_kssa__kiosk_saver UNIQUE (kiosk_id, screen_saver_id)
);

\echo '✓ 0014 kiosk done.'
