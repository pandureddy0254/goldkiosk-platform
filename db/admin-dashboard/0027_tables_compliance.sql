-- ============================================================================
-- 0027_tables_compliance.sql
-- Schema: compliance
-- Tables: retention_policies, data_subject_requests, data_export_jobs,
--         data_deletion_jobs
-- ============================================================================

\echo '── 0027 compliance ──'

-- ─── retention_policies ─────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS compliance.retention_policies (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL,
    data_class      text NOT NULL,
    retention_days  integer NOT NULL,
    disposition     text NOT NULL,
    is_active       boolean NOT NULL DEFAULT true,
    CONSTRAINT fk_retention_policies__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_retention_policies__tenant_class UNIQUE (tenant_id, data_class),
    CONSTRAINT ck_retention_policies__disposition CHECK (disposition IN ('purge','archive','pseudonymise'))
);

-- ─── data_subject_requests ──────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS compliance.data_subject_requests (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id           uuid NOT NULL,
    customer_id         uuid NOT NULL,
    request_type        text NOT NULL,
    status              text NOT NULL DEFAULT 'received',
    received_at         timestamptz NOT NULL DEFAULT now(),
    due_at              timestamptz NOT NULL,
    completed_at        timestamptz NULL,
    approver_user_id    uuid NULL,
    notes               text NULL,
    CONSTRAINT fk_dsr__tenants   FOREIGN KEY (tenant_id)        REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_dsr__customers FOREIGN KEY (customer_id)      REFERENCES customer.customers(id),
    CONSTRAINT fk_dsr__approver  FOREIGN KEY (approver_user_id) REFERENCES identity.users(id),
    CONSTRAINT ck_dsr__type   CHECK (request_type IN ('access','rectification','erasure','portability','restriction')),
    CONSTRAINT ck_dsr__status CHECK (status       IN ('received','verified','in_progress','completed','refused'))
);

CREATE INDEX IF NOT EXISTS ix_dsr__open ON compliance.data_subject_requests(due_at) WHERE status NOT IN ('completed','refused');

-- ─── data_export_jobs ───────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS compliance.data_export_jobs (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    data_subject_request_id  uuid NOT NULL,
    output_blob_uri          text NULL,
    output_hash_sha256       bytea NULL,
    status                   text NOT NULL DEFAULT 'queued',
    started_at               timestamptz NULL,
    completed_at             timestamptz NULL,
    expires_at               timestamptz NULL,
    CONSTRAINT fk_dej__dsr FOREIGN KEY (data_subject_request_id) REFERENCES compliance.data_subject_requests(id) ON DELETE CASCADE,
    CONSTRAINT ck_dej__status CHECK (status IN ('queued','running','completed','failed'))
);

-- ─── data_deletion_jobs ─────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS compliance.data_deletion_jobs (
    id                         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    data_subject_request_id    uuid NULL,
    retention_policy_id        uuid NULL,
    tenant_id                  uuid NOT NULL,
    target_summary_json        jsonb NOT NULL,
    legal_hold_excluded_json   jsonb NULL,
    status                     text NOT NULL DEFAULT 'queued',
    queued_at                  timestamptz NOT NULL DEFAULT now(),
    completed_at               timestamptz NULL,
    rows_pseudonymised         integer NOT NULL DEFAULT 0,
    rows_deleted               integer NOT NULL DEFAULT 0,
    CONSTRAINT fk_ddj__dsr        FOREIGN KEY (data_subject_request_id) REFERENCES compliance.data_subject_requests(id),
    CONSTRAINT fk_ddj__policies   FOREIGN KEY (retention_policy_id)     REFERENCES compliance.retention_policies(id),
    CONSTRAINT fk_ddj__tenants    FOREIGN KEY (tenant_id)               REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT ck_ddj__status     CHECK (status IN ('queued','running','completed','failed')),
    CONSTRAINT ck_ddj__source     CHECK (data_subject_request_id IS NOT NULL OR retention_policy_id IS NOT NULL)
);

\echo '✓ 0027 compliance done.'
