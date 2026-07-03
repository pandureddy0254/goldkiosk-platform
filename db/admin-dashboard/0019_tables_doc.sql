-- ============================================================================
-- 0019_tables_doc.sql
-- Schema: doc
-- Receipts, contracts, templates, signatures, statements.
-- Tables: receipt_templates, contract_templates, invoice_numbers, receipts,
--         contracts, signatures, statements
-- ============================================================================

\echo '── 0019 doc ──'

-- ─── receipt_templates ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS doc.receipt_templates (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id    uuid NOT NULL,
    code         citext NOT NULL,
    version      text NOT NULL,
    body         text NOT NULL,
    locale       public.domain_locale_tag NOT NULL,
    is_active    boolean NOT NULL DEFAULT true,
    created_at   timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_receipt_templates__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_receipt_templates__tenant_code_version UNIQUE (tenant_id, code, version)
);

-- ─── contract_templates ─────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS doc.contract_templates (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL,
    code            citext NOT NULL,
    version         text NOT NULL,
    body            text NOT NULL,
    locale          public.domain_locale_tag NOT NULL,
    jurisdiction    text NOT NULL,
    effective_from  timestamptz NOT NULL DEFAULT now(),
    is_active       boolean NOT NULL DEFAULT true,
    CONSTRAINT fk_contract_templates__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_contract_templates__tenant_code_version UNIQUE (tenant_id, code, version)
);

-- ─── invoice_numbers (gap-less sequence per tenant per code) ────────────────
CREATE TABLE IF NOT EXISTS doc.invoice_numbers (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL,
    sequence_code   text NOT NULL,
    next_value      bigint NOT NULL DEFAULT 1,
    prefix          text NULL,
    CONSTRAINT fk_invoice_numbers__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_invoice_numbers__tenant_sequence UNIQUE (tenant_id, sequence_code)
);

-- ─── receipts ───────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS doc.receipts (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id       uuid NOT NULL,
    receipt_template_id  uuid NOT NULL,
    invoice_number_id    uuid NOT NULL,
    invoice_number       text NOT NULL,
    blob_uri             text NOT NULL,
    hash_sha256          bytea NOT NULL,
    issued_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_receipts__transactions FOREIGN KEY (transaction_id)       REFERENCES tx.transactions(id) ON DELETE CASCADE,
    CONSTRAINT fk_receipts__templates    FOREIGN KEY (receipt_template_id)  REFERENCES doc.receipt_templates(id),
    CONSTRAINT fk_receipts__invoice_nums FOREIGN KEY (invoice_number_id)    REFERENCES doc.invoice_numbers(id),
    CONSTRAINT uq_receipts__transaction UNIQUE (transaction_id),
    CONSTRAINT uq_receipts__invoice_number UNIQUE (invoice_number)
);

-- ─── contracts ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS doc.contracts (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id        uuid NOT NULL,
    contract_template_id  uuid NOT NULL,
    blob_uri              text NOT NULL,
    hash_sha256           bytea NOT NULL,
    signed_at             timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_contracts__transactions FOREIGN KEY (transaction_id)        REFERENCES tx.transactions(id) ON DELETE CASCADE,
    CONSTRAINT fk_contracts__templates    FOREIGN KEY (contract_template_id)  REFERENCES doc.contract_templates(id),
    CONSTRAINT uq_contracts__transaction UNIQUE (transaction_id)
);

-- ─── signatures ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS doc.signatures (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id         uuid NOT NULL,
    image_blob_uri_enc  bytea NOT NULL,
    vector_hash_enc     bytea NULL,
    captured_at         timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_signatures__contracts FOREIGN KEY (contract_id) REFERENCES doc.contracts(id) ON DELETE CASCADE,
    CONSTRAINT uq_signatures__contract UNIQUE (contract_id)
);

-- ─── statements ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS doc.statements (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id   uuid NOT NULL,
    period_start  date NOT NULL,
    period_end    date NOT NULL,
    blob_uri      text NOT NULL,
    hash_sha256   bytea NOT NULL,
    issued_at     timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_statements__customers FOREIGN KEY (customer_id) REFERENCES customer.customers(id) ON DELETE CASCADE,
    CONSTRAINT ck_statements__period CHECK (period_end >= period_start)
);

CREATE INDEX IF NOT EXISTS ix_statements__customer ON doc.statements(customer_id, period_end DESC);

\echo '✓ 0019 doc done.'
