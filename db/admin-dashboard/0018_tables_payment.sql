-- ============================================================================
-- 0018_tables_payment.sql
-- Schema: payment
-- Tables: payment_methods, payment_providers (NOT defined here — see payouts),
--         bank_accounts, payments, payment_instructions, payouts,
--         payout_providers, payout_attempts, refunds, cash_dispense_requests,
--         cash_cassette_inventory
-- ============================================================================

\echo '── 0018 payment ──'

-- ─── payment_methods ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS payment.payment_methods (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id     uuid NOT NULL,
    code          citext NOT NULL,
    display_name  text NOT NULL,
    category      text NOT NULL,
    region_code   text NULL,
    is_active     boolean NOT NULL DEFAULT true,
    CONSTRAINT fk_payment_methods__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_payment_methods__tenant_code UNIQUE (tenant_id, code),
    CONSTRAINT ck_payment_methods__category CHECK (category IN ('cash','bank_transfer','wallet','cheque','store_credit'))
);

-- ─── payout_providers ───────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS payment.payout_providers (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code          citext NOT NULL,
    display_name  text NOT NULL,
    adapter_type  text NOT NULL,
    region_code   text NOT NULL,
    priority      integer NOT NULL DEFAULT 100,
    is_active     boolean NOT NULL DEFAULT true,
    CONSTRAINT uq_payout_providers__code UNIQUE (code)
);

-- ─── bank_accounts ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS payment.bank_accounts (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id           uuid NOT NULL,
    holder_name_enc       bytea NOT NULL,
    iban_enc              bytea NULL,
    account_number_enc    bytea NULL,
    routing_number_enc    bytea NULL,
    swift_bic             text NULL,
    bank_name             text NULL,
    currency_code         public.domain_currency_code NOT NULL,
    is_verified           boolean NOT NULL DEFAULT false,
    created_at            timestamptz NOT NULL DEFAULT now(),
    deleted_at            timestamptz NULL,
    CONSTRAINT fk_bank_accounts__customers FOREIGN KEY (customer_id) REFERENCES customer.customers(id) ON DELETE CASCADE,
    CONSTRAINT fk_bank_accounts__currencies FOREIGN KEY (currency_code) REFERENCES tenancy.currencies(code)
);

CREATE INDEX IF NOT EXISTS ix_bank_accounts__customer ON payment.bank_accounts(customer_id) WHERE deleted_at IS NULL;

-- ─── payments ───────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS payment.payments (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id      uuid NOT NULL,
    payment_method_id   uuid NOT NULL,
    amount              public.domain_money NOT NULL,
    currency_code       public.domain_currency_code NOT NULL,
    status              text NOT NULL DEFAULT 'pending',
    initiated_at        timestamptz NULL,
    completed_at        timestamptz NULL,
    failed_at           timestamptz NULL,
    CONSTRAINT fk_payments__transactions FOREIGN KEY (transaction_id)    REFERENCES tx.transactions(id) ON DELETE CASCADE,
    CONSTRAINT fk_payments__methods      FOREIGN KEY (payment_method_id) REFERENCES payment.payment_methods(id),
    CONSTRAINT fk_payments__currencies   FOREIGN KEY (currency_code)     REFERENCES tenancy.currencies(code),
    CONSTRAINT ck_payments__status CHECK (status IN ('pending','initiated','completed','failed','cancelled'))
);

CREATE INDEX IF NOT EXISTS ix_payments__transaction ON payment.payments(transaction_id);

-- ─── payment_instructions ───────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS payment.payment_instructions (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_id          uuid NOT NULL,
    bank_account_id     uuid NULL,
    wallet_handle_enc   bytea NULL,
    reference_text      text NULL,
    CONSTRAINT fk_payment_instructions__payments       FOREIGN KEY (payment_id)      REFERENCES payment.payments(id) ON DELETE CASCADE,
    CONSTRAINT fk_payment_instructions__bank_accounts  FOREIGN KEY (bank_account_id) REFERENCES payment.bank_accounts(id),
    CONSTRAINT uq_payment_instructions__payment UNIQUE (payment_id)
);

-- ─── payouts ────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS payment.payouts (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_id           uuid NOT NULL,
    payout_provider_id   uuid NOT NULL,
    external_reference   text NULL,
    status               text NOT NULL DEFAULT 'queued',
    fee_amount           public.domain_money NOT NULL DEFAULT 0,
    sent_at              timestamptz NULL,
    settled_at           timestamptz NULL,
    CONSTRAINT fk_payouts__payments  FOREIGN KEY (payment_id)         REFERENCES payment.payments(id) ON DELETE CASCADE,
    CONSTRAINT fk_payouts__providers FOREIGN KEY (payout_provider_id) REFERENCES payment.payout_providers(id),
    CONSTRAINT ck_payouts__status CHECK (status IN ('queued','sent','settled','failed','returned'))
);

CREATE INDEX IF NOT EXISTS ix_payouts__payment ON payment.payouts(payment_id);
CREATE INDEX IF NOT EXISTS ix_payouts__status  ON payment.payouts(status) WHERE status IN ('queued','sent');

-- ─── payout_attempts (append-only) ──────────────────────────────────────────
CREATE TABLE IF NOT EXISTS payment.payout_attempts (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payout_id              uuid NOT NULL,
    attempt_no             integer NOT NULL,
    idempotency_key        text NOT NULL,
    request_hash           bytea NOT NULL,
    response_code          integer NULL,
    response_body_excerpt  text NULL,
    status                 text NOT NULL,
    latency_ms             integer NULL,
    attempted_at           timestamptz NOT NULL DEFAULT now(),
    next_retry_at          timestamptz NULL,
    CONSTRAINT fk_payout_attempts__payouts FOREIGN KEY (payout_id) REFERENCES payment.payouts(id) ON DELETE CASCADE,
    CONSTRAINT uq_payout_attempts__payout_attempt UNIQUE (payout_id, attempt_no),
    CONSTRAINT uq_payout_attempts__idempotency UNIQUE (idempotency_key),
    CONSTRAINT ck_payout_attempts__status CHECK (status IN ('success','retry','fail'))
);

-- ─── refunds ────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS payment.refunds (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_id            uuid NOT NULL,
    amount                public.domain_money NOT NULL,
    currency_code         public.domain_currency_code NOT NULL,
    reason                text NOT NULL,
    status                text NOT NULL DEFAULT 'requested',
    requested_by_user_id  uuid NOT NULL,
    approved_by_user_id   uuid NULL,
    issued_at             timestamptz NULL,
    CONSTRAINT fk_refunds__payments    FOREIGN KEY (payment_id)            REFERENCES payment.payments(id) ON DELETE CASCADE,
    CONSTRAINT fk_refunds__requester   FOREIGN KEY (requested_by_user_id)  REFERENCES identity.users(id),
    CONSTRAINT fk_refunds__approver    FOREIGN KEY (approved_by_user_id)   REFERENCES identity.users(id),
    CONSTRAINT fk_refunds__currencies  FOREIGN KEY (currency_code)         REFERENCES tenancy.currencies(code),
    CONSTRAINT ck_refunds__status CHECK (status IN ('requested','approved','issued','declined'))
);

-- ─── cash_dispense_requests ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS payment.cash_dispense_requests (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_id      uuid NOT NULL,
    kiosk_id        uuid NOT NULL,
    amount          public.domain_money NOT NULL,
    currency_code   public.domain_currency_code NOT NULL,
    plan_json       jsonb NOT NULL,
    actual_json     jsonb NULL,
    status          text NOT NULL DEFAULT 'requested',
    requested_at    timestamptz NOT NULL DEFAULT now(),
    dispensed_at    timestamptz NULL,
    CONSTRAINT fk_cdr__payments FOREIGN KEY (payment_id) REFERENCES payment.payments(id) ON DELETE CASCADE,
    CONSTRAINT fk_cdr__kiosks   FOREIGN KEY (kiosk_id)   REFERENCES kiosk.kiosks(id),
    CONSTRAINT uq_cdr__payment UNIQUE (payment_id),
    CONSTRAINT ck_cdr__status CHECK (status IN ('requested','dispensing','dispensed','jammed','short_paid'))
);

-- ─── cash_cassette_inventory ────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS payment.cash_cassette_inventory (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    kiosk_id            uuid NOT NULL,
    cassette_position   smallint NOT NULL,
    denomination_minor  integer NOT NULL,
    currency_code       public.domain_currency_code NOT NULL,
    expected_count      integer NOT NULL DEFAULT 0,
    actual_count        integer NULL,
    last_refilled_at    timestamptz NULL,
    reconciled_at       timestamptz NULL,
    CONSTRAINT fk_cci__kiosks     FOREIGN KEY (kiosk_id)      REFERENCES kiosk.kiosks(id) ON DELETE CASCADE,
    CONSTRAINT fk_cci__currencies FOREIGN KEY (currency_code) REFERENCES tenancy.currencies(code),
    CONSTRAINT uq_cci__kiosk_position UNIQUE (kiosk_id, cassette_position)
);

\echo '✓ 0018 payment done.'
