-- ============================================================================
-- 0016_tables_tx.sql
-- Schema: tx
-- Customer transactions and item analysis.
-- Tables: rejection_reasons, transactions, transaction_events,
--         transaction_snapshots, transaction_documents, transaction_audit_trail,
--         transaction_items, item_analyses, item_photographs, karat_results,
--         weight_readings, pawn_loans
-- ============================================================================

\echo '── 0016 tx ──'

-- ─── rejection_reasons (master) ─────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tx.rejection_reasons (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code          citext NOT NULL,
    display_name  text NOT NULL,
    category      text NOT NULL,
    is_active     boolean NOT NULL DEFAULT true,
    CONSTRAINT uq_rejection_reasons__code UNIQUE (code),
    CONSTRAINT ck_rejection_reasons__category CHECK (category IN ('customer','pricing','kyc','device','fraud'))
);

-- ─── transactions ───────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tx.transactions (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id             uuid NOT NULL,
    kiosk_id              uuid NOT NULL,
    store_id              uuid NOT NULL,
    customer_id           uuid NOT NULL,
    transaction_code      citext NOT NULL,
    kind                  text NOT NULL,
    status                text NOT NULL DEFAULT 'started',
    total_amount          public.domain_money NOT NULL DEFAULT 0,
    fees                  public.domain_money NOT NULL DEFAULT 0,
    net_payout            public.domain_money NOT NULL DEFAULT 0,
    currency_code         public.domain_currency_code NOT NULL,
    source                text NULL,
    coordinator_user_id   uuid NULL,
    rejection_reason_id   uuid NULL,
    is_junk               boolean NOT NULL DEFAULT false,
    started_at            timestamptz NOT NULL DEFAULT now(),
    completed_at          timestamptz NULL,
    occurred_at           timestamptz NOT NULL DEFAULT now(),
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_transactions__tenants    FOREIGN KEY (tenant_id)            REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_transactions__kiosks     FOREIGN KEY (kiosk_id)             REFERENCES kiosk.kiosks(id),
    CONSTRAINT fk_transactions__stores     FOREIGN KEY (store_id)             REFERENCES store.stores(id),
    CONSTRAINT fk_transactions__customers  FOREIGN KEY (customer_id)          REFERENCES customer.customers(id),
    CONSTRAINT fk_transactions__users      FOREIGN KEY (coordinator_user_id)  REFERENCES identity.users(id),
    CONSTRAINT fk_transactions__rejections FOREIGN KEY (rejection_reason_id)  REFERENCES tx.rejection_reasons(id),
    CONSTRAINT fk_transactions__currencies FOREIGN KEY (currency_code)        REFERENCES tenancy.currencies(code),
    CONSTRAINT uq_transactions__tenant_code UNIQUE (tenant_id, transaction_code),
    CONSTRAINT ck_transactions__kind   CHECK (kind   IN ('precious_sale','pawn','voucher_redemption','payout','refund','crypto_buy')),
    CONSTRAINT ck_transactions__status CHECK (status IN ('started','weighing','analysing','offered','accepted','paid','declined','aborted'))
);

CREATE INDEX IF NOT EXISTS ix_transactions__tenant_occurred ON tx.transactions(tenant_id, occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_transactions__kiosk_occurred  ON tx.transactions(kiosk_id, occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_transactions__customer        ON tx.transactions(customer_id);
CREATE INDEX IF NOT EXISTS ix_transactions__status          ON tx.transactions(tenant_id, status);

-- ─── transaction_events (append-only, partitioned) ──────────────────────────
CREATE TABLE IF NOT EXISTS tx.transaction_events (
    sequence_no       bigint GENERATED ALWAYS AS IDENTITY,
    id                uuid NOT NULL DEFAULT gen_random_uuid(),
    transaction_id    uuid NOT NULL,
    event_type        text NOT NULL,
    payload_json      jsonb NOT NULL,
    actor             text NOT NULL,
    actor_user_id     uuid NULL,
    correlation_id    uuid NULL,
    occurred_at       timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT pk_transaction_events PRIMARY KEY (sequence_no, occurred_at),
    CONSTRAINT ck_transaction_events__actor CHECK (actor IN ('customer','agent','kiosk','system'))
) PARTITION BY RANGE (occurred_at);

CREATE TABLE IF NOT EXISTS tx.transaction_events_default PARTITION OF tx.transaction_events DEFAULT;

CREATE INDEX IF NOT EXISTS ix_transaction_events__transaction ON tx.transaction_events(transaction_id, occurred_at);

-- ─── transaction_snapshots ──────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tx.transaction_snapshots (
    transaction_id      uuid PRIMARY KEY,
    status              text NOT NULL,
    items_count         integer NOT NULL DEFAULT 0,
    total_amount        public.domain_money NOT NULL DEFAULT 0,
    currency_code       public.domain_currency_code NOT NULL,
    latest_event_type   text NOT NULL,
    latest_event_at     timestamptz NOT NULL,
    projected_at        timestamptz NOT NULL DEFAULT now(),
    state_json          jsonb NULL,
    CONSTRAINT fk_transaction_snapshots__transactions FOREIGN KEY (transaction_id) REFERENCES tx.transactions(id) ON DELETE CASCADE
);

-- ─── transaction_documents ──────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tx.transaction_documents (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id  uuid NOT NULL,
    document_type   text NOT NULL,
    blob_uri        text NOT NULL,
    hash_sha256     bytea NOT NULL,
    mime_type       text NOT NULL,
    size_bytes      integer NOT NULL,
    issued_at       timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_transaction_documents__transactions FOREIGN KEY (transaction_id) REFERENCES tx.transactions(id) ON DELETE CASCADE,
    CONSTRAINT ck_transaction_documents__type CHECK (document_type IN ('bag_scan','receipt','contract','id_capture','signature','other'))
);

-- ─── transaction_audit_trail ────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tx.transaction_audit_trail (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id  uuid NOT NULL,
    action          text NOT NULL,
    actor_user_id   uuid NULL,
    actor_label     text NOT NULL,
    before_json     jsonb NULL,
    after_json      jsonb NULL,
    reason          text NULL,
    at              timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_transaction_audit_trail__transactions FOREIGN KEY (transaction_id) REFERENCES tx.transactions(id) ON DELETE CASCADE,
    CONSTRAINT fk_transaction_audit_trail__users        FOREIGN KEY (actor_user_id)  REFERENCES identity.users(id)
);

CREATE INDEX IF NOT EXISTS ix_transaction_audit_trail__transaction ON tx.transaction_audit_trail(transaction_id, at DESC);

-- ─── transaction_items ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tx.transaction_items (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id      uuid NOT NULL,
    item_index          smallint NOT NULL,
    item_type           text NOT NULL,
    metal               text NOT NULL,
    declared_karat      public.domain_karat NULL,
    declared_weight_g   public.domain_grams NULL,
    billed_weight_g     public.domain_grams NOT NULL DEFAULT 0,
    billed_karat        public.domain_karat NOT NULL,
    description         text NULL,
    created_at          timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_transaction_items__transactions FOREIGN KEY (transaction_id) REFERENCES tx.transactions(id) ON DELETE CASCADE,
    CONSTRAINT uq_transaction_items__transaction_index UNIQUE (transaction_id, item_index),
    CONSTRAINT ck_transaction_items__metal CHECK (metal IN ('gold','silver','platinum','palladium'))
);

CREATE INDEX IF NOT EXISTS ix_transaction_items__transaction ON tx.transaction_items(transaction_id);

-- ─── item_analyses ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tx.item_analyses (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_item_id uuid NOT NULL,
    measured_karat      numeric(5,2) NOT NULL,
    confidence          numeric(5,4) NULL,
    xrf_readings_json   jsonb NOT NULL,
    has_anomaly         boolean NOT NULL DEFAULT false,
    analyzer_serial     text NOT NULL,
    analyzed_at         timestamptz NOT NULL DEFAULT now(),
    volume_ml           numeric(10,4) NULL,
    density_g_per_ml    numeric(8,4) NULL,
    CONSTRAINT fk_item_analyses__items FOREIGN KEY (transaction_item_id) REFERENCES tx.transaction_items(id) ON DELETE CASCADE,
    CONSTRAINT uq_item_analyses__item UNIQUE (transaction_item_id)
);

-- ─── item_photographs ───────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tx.item_photographs (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_item_id uuid NOT NULL,
    stage               text NOT NULL,
    blob_uri            text NOT NULL,
    hash_sha256         bytea NOT NULL,
    captured_at         timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_item_photographs__items FOREIGN KEY (transaction_item_id) REFERENCES tx.transaction_items(id) ON DELETE CASCADE,
    CONSTRAINT ck_item_photographs__stage CHECK (stage IN ('intake','weighing','analysis','disposition','overview'))
);

-- ─── karat_results ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tx.karat_results (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    item_analysis_id     uuid NOT NULL,
    declared_karat       public.domain_karat NULL,
    measured_karat       numeric(5,2) NOT NULL,
    billed_karat         public.domain_karat NOT NULL,
    resolution_mode      text NOT NULL,
    approved_by_user_id  uuid NULL,
    approved_at          timestamptz NULL,
    CONSTRAINT fk_karat_results__analyses FOREIGN KEY (item_analysis_id) REFERENCES tx.item_analyses(id) ON DELETE CASCADE,
    CONSTRAINT fk_karat_results__users    FOREIGN KEY (approved_by_user_id) REFERENCES identity.users(id),
    CONSTRAINT uq_karat_results__analysis UNIQUE (item_analysis_id),
    CONSTRAINT ck_karat_results__mode CHECK (resolution_mode IN ('auto_match','auto_use_lower','manual_override'))
);

-- ─── weight_readings (append-only) ──────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tx.weight_readings (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_item_id   uuid NOT NULL,
    reading_type          text NOT NULL,
    grams                 public.domain_grams NOT NULL,
    scale_serial          text NOT NULL,
    measured_at           timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_weight_readings__items FOREIGN KEY (transaction_item_id) REFERENCES tx.transaction_items(id) ON DELETE CASCADE,
    CONSTRAINT ck_weight_readings__type CHECK (reading_type IN ('gross','with_stones','without_stones','tare'))
);

CREATE INDEX IF NOT EXISTS ix_weight_readings__item ON tx.weight_readings(transaction_item_id, measured_at);

-- ─── pawn_loans ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tx.pawn_loans (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id      uuid NOT NULL,
    principal           public.domain_money NOT NULL,
    currency_code       public.domain_currency_code NOT NULL,
    interest_rate_pct   public.domain_percent NOT NULL,
    term_days           integer NOT NULL,
    due_date            date NOT NULL,
    status              text NOT NULL DEFAULT 'active',
    redemption_amount   public.domain_money NULL,
    redeemed_at         timestamptz NULL,
    forfeited_at        timestamptz NULL,
    CONSTRAINT fk_pawn_loans__transactions FOREIGN KEY (transaction_id) REFERENCES tx.transactions(id) ON DELETE CASCADE,
    CONSTRAINT fk_pawn_loans__currencies   FOREIGN KEY (currency_code)  REFERENCES tenancy.currencies(code),
    CONSTRAINT uq_pawn_loans__transaction UNIQUE (transaction_id),
    CONSTRAINT ck_pawn_loans__status CHECK (status IN ('active','redeemed','forfeited','extended'))
);

CREATE INDEX IF NOT EXISTS ix_pawn_loans__due ON tx.pawn_loans(due_date) WHERE status = 'active';

\echo '✓ 0016 tx done.'
