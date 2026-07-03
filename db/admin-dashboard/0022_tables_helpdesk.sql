-- ============================================================================
-- 0022_tables_helpdesk.sql
-- Schema: helpdesk
-- Tables: ticket_categories, ticket_sub_categories, feedback, support_tickets,
--         sos_requests
-- ============================================================================

\echo '── 0022 helpdesk ──'

-- ─── ticket_categories ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS helpdesk.ticket_categories (
    id         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code       citext NOT NULL,
    name       text NOT NULL,
    is_active  boolean NOT NULL DEFAULT true,
    CONSTRAINT uq_ticket_categories__code UNIQUE (code)
);

-- ─── ticket_sub_categories ──────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS helpdesk.ticket_sub_categories (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    parent_category_id  uuid NOT NULL,
    code                citext NOT NULL,
    name                text NOT NULL,
    is_active           boolean NOT NULL DEFAULT true,
    CONSTRAINT fk_ticket_sub_categories__parent FOREIGN KEY (parent_category_id) REFERENCES helpdesk.ticket_categories(id) ON DELETE CASCADE,
    CONSTRAINT uq_ticket_sub_categories__parent_code UNIQUE (parent_category_id, code)
);

-- ─── feedback ───────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS helpdesk.feedback (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL,
    kiosk_id        uuid NULL,
    customer_id     uuid NULL,
    function_name   text NOT NULL,
    feedback_type   text NOT NULL,
    description     text NULL,
    audio_url       text NULL,
    image_url       text NULL,
    location_text   text NULL,
    is_read         boolean NOT NULL DEFAULT false,
    created_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_feedback__tenants   FOREIGN KEY (tenant_id)   REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_feedback__kiosks    FOREIGN KEY (kiosk_id)    REFERENCES kiosk.kiosks(id),
    CONSTRAINT fk_feedback__customers FOREIGN KEY (customer_id) REFERENCES customer.customers(id),
    CONSTRAINT ck_feedback__type CHECK (feedback_type IN ('compliment','complaint','suggestion','question'))
);

CREATE INDEX IF NOT EXISTS ix_feedback__tenant_time ON helpdesk.feedback(tenant_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_feedback__unread ON helpdesk.feedback(tenant_id) WHERE is_read = false;

-- ─── support_tickets ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS helpdesk.support_tickets (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                uuid NOT NULL,
    code                     citext NOT NULL,
    category_id              uuid NOT NULL,
    sub_category_id          uuid NULL,
    description              text NOT NULL,
    status                   text NOT NULL DEFAULT 'open',
    remarks                  text NULL,
    documents_uri            text NULL,
    created_by_user_id       uuid NOT NULL,
    closed_by_user_id        uuid NULL,
    reopened_by_user_id      uuid NULL,
    is_closed                boolean NOT NULL DEFAULT false,
    is_reopened              boolean NOT NULL DEFAULT false,
    created_at               timestamptz NOT NULL DEFAULT now(),
    closed_at                timestamptz NULL,
    reopened_at              timestamptz NULL,
    CONSTRAINT fk_support_tickets__tenants    FOREIGN KEY (tenant_id)           REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_support_tickets__category   FOREIGN KEY (category_id)         REFERENCES helpdesk.ticket_categories(id),
    CONSTRAINT fk_support_tickets__subcat     FOREIGN KEY (sub_category_id)     REFERENCES helpdesk.ticket_sub_categories(id),
    CONSTRAINT fk_support_tickets__creator    FOREIGN KEY (created_by_user_id)  REFERENCES identity.users(id),
    CONSTRAINT fk_support_tickets__closer     FOREIGN KEY (closed_by_user_id)   REFERENCES identity.users(id),
    CONSTRAINT fk_support_tickets__reopener   FOREIGN KEY (reopened_by_user_id) REFERENCES identity.users(id),
    CONSTRAINT uq_support_tickets__tenant_code UNIQUE (tenant_id, code),
    CONSTRAINT ck_support_tickets__status CHECK (status IN ('open','in_progress','waiting_customer','resolved','closed','reopened'))
);

CREATE INDEX IF NOT EXISTS ix_support_tickets__tenant_status ON helpdesk.support_tickets(tenant_id, status);

-- ─── sos_requests ───────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS helpdesk.sos_requests (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                uuid NOT NULL,
    kiosk_id                 uuid NULL,
    customer_id              uuid NULL,
    priority                 text NOT NULL DEFAULT 'high',
    description              text NULL,
    status                   text NOT NULL DEFAULT 'raised',
    assigned_technician_id   uuid NULL,
    raised_at                timestamptz NOT NULL DEFAULT now(),
    acknowledged_at          timestamptz NULL,
    resolved_at              timestamptz NULL,
    CONSTRAINT fk_sos_requests__tenants     FOREIGN KEY (tenant_id)              REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_sos_requests__kiosks      FOREIGN KEY (kiosk_id)               REFERENCES kiosk.kiosks(id),
    CONSTRAINT fk_sos_requests__customers   FOREIGN KEY (customer_id)            REFERENCES customer.customers(id),
    CONSTRAINT fk_sos_requests__technicians FOREIGN KEY (assigned_technician_id) REFERENCES ops.technicians(id),
    CONSTRAINT ck_sos_requests__priority CHECK (priority IN ('low','medium','high','critical')),
    CONSTRAINT ck_sos_requests__status   CHECK (status   IN ('raised','acknowledged','dispatched','resolved','cancelled'))
);

CREATE INDEX IF NOT EXISTS ix_sos_requests__open ON helpdesk.sos_requests(raised_at DESC) WHERE status IN ('raised','acknowledged','dispatched');

\echo '✓ 0022 helpdesk done.'
