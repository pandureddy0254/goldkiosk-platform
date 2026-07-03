-- ============================================================================
-- 0020_tables_ops.sql
-- Schema: ops
-- Field operations: technicians, tickets, snapshots.
-- Tables: technician_clusters, technicians, deployment_tickets,
--         maintenance_tickets, ticket_status_history, collection_runs,
--         collection_tickets, kiosk_inventory_snapshots, system_health_snapshots
-- ============================================================================

\echo '── 0020 ops ──'

-- ─── technician_clusters ────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ops.technician_clusters (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   uuid NOT NULL,
    code        citext NOT NULL,
    name        text NOT NULL,
    is_active   boolean NOT NULL DEFAULT true,
    CONSTRAINT fk_technician_clusters__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_technician_clusters__tenant_code UNIQUE (tenant_id, code)
);

-- ─── technicians ────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ops.technicians (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   uuid NOT NULL,
    user_id     uuid NULL,
    name        text NOT NULL,
    email_enc   bytea NULL,
    mobile_enc  bytea NULL,
    cluster_id  uuid NOT NULL,
    status      text NOT NULL DEFAULT 'active',
    is_active   boolean NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_technicians__tenants  FOREIGN KEY (tenant_id)  REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_technicians__users    FOREIGN KEY (user_id)    REFERENCES identity.users(id),
    CONSTRAINT fk_technicians__clusters FOREIGN KEY (cluster_id) REFERENCES ops.technician_clusters(id),
    CONSTRAINT ck_technicians__status CHECK (status IN ('active','on_leave','inactive'))
);

CREATE INDEX IF NOT EXISTS ix_technicians__cluster ON ops.technicians(cluster_id) WHERE is_active = true;

-- ─── deployment_tickets ─────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ops.deployment_tickets (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                uuid NOT NULL,
    code                     citext NOT NULL,
    kiosk_id                 uuid NULL,
    store_id                 uuid NULL,
    status                   text NOT NULL DEFAULT 'open',
    priority                 text NOT NULL DEFAULT 'normal',
    description              text NOT NULL,
    assigned_technician_id   uuid NULL,
    opened_at                timestamptz NOT NULL DEFAULT now(),
    closed_at                timestamptz NULL,
    CONSTRAINT fk_deployment_tickets__tenants     FOREIGN KEY (tenant_id)              REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_deployment_tickets__kiosks      FOREIGN KEY (kiosk_id)               REFERENCES kiosk.kiosks(id),
    CONSTRAINT fk_deployment_tickets__stores      FOREIGN KEY (store_id)               REFERENCES store.stores(id),
    CONSTRAINT fk_deployment_tickets__technicians FOREIGN KEY (assigned_technician_id) REFERENCES ops.technicians(id),
    CONSTRAINT uq_deployment_tickets__tenant_code UNIQUE (tenant_id, code),
    CONSTRAINT ck_deployment_tickets__status   CHECK (status   IN ('open','in_progress','resolved','closed','reopened','other')),
    CONSTRAINT ck_deployment_tickets__priority CHECK (priority IN ('low','normal','high','urgent'))
);

CREATE INDEX IF NOT EXISTS ix_deployment_tickets__status ON ops.deployment_tickets(tenant_id, status);

-- ─── maintenance_tickets ────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ops.maintenance_tickets (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL,
    code            citext NOT NULL,
    kiosk_id        uuid NOT NULL,
    device_id       uuid NULL,
    ticket_type     text NOT NULL,
    description     text NOT NULL,
    priority        text NOT NULL DEFAULT 'normal',
    status          text NOT NULL DEFAULT 'open',
    technician_id   uuid NULL,
    scheduled_at    timestamptz NULL,
    completed_at    timestamptz NULL,
    CONSTRAINT fk_maintenance_tickets__tenants     FOREIGN KEY (tenant_id)     REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_maintenance_tickets__kiosks      FOREIGN KEY (kiosk_id)      REFERENCES kiosk.kiosks(id),
    CONSTRAINT fk_maintenance_tickets__devices     FOREIGN KEY (device_id)     REFERENCES kiosk.devices(id),
    CONSTRAINT fk_maintenance_tickets__technicians FOREIGN KEY (technician_id) REFERENCES ops.technicians(id),
    CONSTRAINT uq_maintenance_tickets__tenant_code UNIQUE (tenant_id, code),
    CONSTRAINT ck_maintenance_tickets__type     CHECK (ticket_type IN ('preventive','corrective','calibration','cleaning')),
    CONSTRAINT ck_maintenance_tickets__status   CHECK (status   IN ('open','in_progress','resolved','closed','reopened','other')),
    CONSTRAINT ck_maintenance_tickets__priority CHECK (priority IN ('low','normal','high','urgent'))
);

CREATE INDEX IF NOT EXISTS ix_maintenance_tickets__kiosk ON ops.maintenance_tickets(kiosk_id, status);

-- ─── ticket_status_history (cross-ticket) ───────────────────────────────────
CREATE TABLE IF NOT EXISTS ops.ticket_status_history (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id           uuid NOT NULL,
    ticket_kind         text NOT NULL,
    old_status          text NULL,
    new_status          text NOT NULL,
    changed_by_user_id  uuid NULL,
    changed_at          timestamptz NOT NULL DEFAULT now(),
    remarks             text NULL,
    CONSTRAINT fk_ticket_status_history__users FOREIGN KEY (changed_by_user_id) REFERENCES identity.users(id),
    CONSTRAINT ck_ticket_status_history__kind CHECK (ticket_kind IN ('deployment','maintenance','support','sos','collection'))
);

CREATE INDEX IF NOT EXISTS ix_ticket_status_history__ticket ON ops.ticket_status_history(ticket_kind, ticket_id, changed_at);

-- ─── collection_runs ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ops.collection_runs (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id            uuid NOT NULL,
    code                 citext NOT NULL,
    run_date             date NOT NULL,
    lead_technician_id   uuid NULL,
    status               text NOT NULL DEFAULT 'planned',
    total_amount         public.domain_money NOT NULL DEFAULT 0,
    total_items          integer NOT NULL DEFAULT 0,
    started_at           timestamptz NULL,
    completed_at         timestamptz NULL,
    CONSTRAINT fk_collection_runs__tenants     FOREIGN KEY (tenant_id)           REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_collection_runs__technicians FOREIGN KEY (lead_technician_id)  REFERENCES ops.technicians(id),
    CONSTRAINT uq_collection_runs__tenant_code UNIQUE (tenant_id, code),
    CONSTRAINT ck_collection_runs__status CHECK (status IN ('planned','in_progress','completed','cancelled'))
);

-- ─── collection_tickets ─────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ops.collection_tickets (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    collection_run_id    uuid NOT NULL,
    kiosk_id             uuid NOT NULL,
    amount_collected     public.domain_money NOT NULL DEFAULT 0,
    currency_code        public.domain_currency_code NOT NULL,
    items_collected      integer NOT NULL DEFAULT 0,
    status               text NOT NULL DEFAULT 'pending',
    collected_at         timestamptz NULL,
    signed_off_by_user_id uuid NULL,
    CONSTRAINT fk_collection_tickets__runs       FOREIGN KEY (collection_run_id)    REFERENCES ops.collection_runs(id) ON DELETE CASCADE,
    CONSTRAINT fk_collection_tickets__kiosks     FOREIGN KEY (kiosk_id)             REFERENCES kiosk.kiosks(id),
    CONSTRAINT fk_collection_tickets__currencies FOREIGN KEY (currency_code)        REFERENCES tenancy.currencies(code),
    CONSTRAINT fk_collection_tickets__users      FOREIGN KEY (signed_off_by_user_id) REFERENCES identity.users(id),
    CONSTRAINT ck_collection_tickets__status CHECK (status IN ('pending','collected','skipped','discrepancy'))
);

CREATE INDEX IF NOT EXISTS ix_collection_tickets__run ON ops.collection_tickets(collection_run_id);

-- ─── kiosk_inventory_snapshots ──────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ops.kiosk_inventory_snapshots (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    kiosk_id    uuid NOT NULL,
    captured_at timestamptz NOT NULL DEFAULT now(),
    metal       text NOT NULL,
    weight_g    public.domain_grams NOT NULL,
    carat       numeric(5,2) NULL,
    CONSTRAINT fk_kiosk_inventory_snapshots__kiosks FOREIGN KEY (kiosk_id) REFERENCES kiosk.kiosks(id) ON DELETE CASCADE,
    CONSTRAINT ck_kiosk_inventory_snapshots__metal CHECK (metal IN ('gold','silver','platinum','palladium'))
);

CREATE INDEX IF NOT EXISTS ix_kiosk_inventory_snapshots__kiosk_time ON ops.kiosk_inventory_snapshots(kiosk_id, captured_at DESC);

-- ─── system_health_snapshots ────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS ops.system_health_snapshots (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id             uuid NOT NULL,
    captured_at           timestamptz NOT NULL DEFAULT now(),
    total_cities          integer NOT NULL DEFAULT 0,
    total_kiosks          integer NOT NULL DEFAULT 0,
    functional_count      integer NOT NULL DEFAULT 0,
    non_functional_count  integer NOT NULL DEFAULT 0,
    avg_uptime_pct        public.domain_percent NOT NULL DEFAULT 0,
    CONSTRAINT fk_system_health_snapshots__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_system_health_snapshots__tenant_time ON ops.system_health_snapshots(tenant_id, captured_at DESC);

\echo '✓ 0020 ops done.'
