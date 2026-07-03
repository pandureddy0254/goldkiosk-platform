-- ============================================================================
-- 0023_tables_merchant.sql
-- Schema: merchant
-- Franchise / B2B partner merchants.
-- Tables: merchants, merchant_bank_accounts, franchise_wallets,
--         wallet_topup_requests, franchise_wallet_ledger
-- ============================================================================

\echo '── 0023 merchant ──'

-- ─── merchants ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS merchant.merchants (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id     uuid NOT NULL,
    code          citext NOT NULL,
    name          text NOT NULL,
    email_enc     bytea NULL,
    mobile_enc    bytea NULL,
    address_enc   bytea NULL,
    kyc_status    text NOT NULL DEFAULT 'pending',
    is_active     boolean NOT NULL DEFAULT true,
    created_at    timestamptz NOT NULL DEFAULT now(),
    deleted_at    timestamptz NULL,
    CONSTRAINT fk_merchants__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_merchants__tenant_code UNIQUE (tenant_id, code),
    CONSTRAINT ck_merchants__kyc_status CHECK (kyc_status IN ('pending','approved','rejected','review'))
);

-- ─── merchant_bank_accounts ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS merchant.merchant_bank_accounts (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id         uuid NOT NULL,
    holder_name_enc     bytea NOT NULL,
    iban_enc            bytea NULL,
    account_number_enc  bytea NULL,
    swift_bic           text NULL,
    bank_name           text NULL,
    currency_code       public.domain_currency_code NOT NULL,
    is_verified         boolean NOT NULL DEFAULT false,
    CONSTRAINT fk_mba__merchants  FOREIGN KEY (merchant_id)   REFERENCES merchant.merchants(id) ON DELETE CASCADE,
    CONSTRAINT fk_mba__currencies FOREIGN KEY (currency_code) REFERENCES tenancy.currencies(code)
);

-- ─── franchise_wallets ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS merchant.franchise_wallets (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    merchant_id       uuid NOT NULL,
    balance           public.domain_money NOT NULL DEFAULT 0,
    currency_code     public.domain_currency_code NOT NULL,
    last_topup_at     timestamptz NULL,
    topup_threshold   public.domain_money NULL,
    created_at        timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_franchise_wallets__merchants  FOREIGN KEY (merchant_id)   REFERENCES merchant.merchants(id) ON DELETE CASCADE,
    CONSTRAINT fk_franchise_wallets__currencies FOREIGN KEY (currency_code) REFERENCES tenancy.currencies(code),
    CONSTRAINT uq_franchise_wallets__merchant_currency UNIQUE (merchant_id, currency_code)
);

-- ─── wallet_topup_requests ──────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS merchant.wallet_topup_requests (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    wallet_id              uuid NOT NULL,
    requested_amount       public.domain_money NOT NULL,
    currency_code          public.domain_currency_code NOT NULL,
    status                 text NOT NULL DEFAULT 'pending',
    payment_reference      text NULL,
    requested_at           timestamptz NOT NULL DEFAULT now(),
    approved_by_user_id    uuid NULL,
    approved_at            timestamptz NULL,
    rejected_reason        text NULL,
    CONSTRAINT fk_wtr__wallets    FOREIGN KEY (wallet_id)           REFERENCES merchant.franchise_wallets(id) ON DELETE CASCADE,
    CONSTRAINT fk_wtr__currencies FOREIGN KEY (currency_code)       REFERENCES tenancy.currencies(code),
    CONSTRAINT fk_wtr__approver   FOREIGN KEY (approved_by_user_id) REFERENCES identity.users(id),
    CONSTRAINT ck_wtr__status CHECK (status IN ('pending','approved','rejected','cancelled'))
);

CREATE INDEX IF NOT EXISTS ix_wallet_topup_requests__pending ON merchant.wallet_topup_requests(requested_at DESC) WHERE status = 'pending';

-- ─── franchise_wallet_ledger (append-only) ──────────────────────────────────
CREATE TABLE IF NOT EXISTS merchant.franchise_wallet_ledger (
    sequence_no        bigint GENERATED ALWAYS AS IDENTITY,
    id                 uuid NOT NULL DEFAULT gen_random_uuid(),
    wallet_id          uuid NOT NULL,
    entry_kind         text NOT NULL,
    amount             public.domain_money NOT NULL,
    balance_after      public.domain_money NOT NULL,
    reference_type     text NULL,
    reference_id       uuid NULL,
    occurred_at        timestamptz NOT NULL DEFAULT now(),
    posted_by_user_id  uuid NULL,
    CONSTRAINT pk_franchise_wallet_ledger PRIMARY KEY (sequence_no),
    CONSTRAINT uq_franchise_wallet_ledger__id UNIQUE (id),
    CONSTRAINT fk_franchise_wallet_ledger__wallets FOREIGN KEY (wallet_id) REFERENCES merchant.franchise_wallets(id) ON DELETE CASCADE,
    CONSTRAINT fk_franchise_wallet_ledger__users   FOREIGN KEY (posted_by_user_id) REFERENCES identity.users(id),
    CONSTRAINT ck_franchise_wallet_ledger__kind CHECK (entry_kind IN ('topup','debit','adjustment','refund','commission'))
);

CREATE INDEX IF NOT EXISTS ix_franchise_wallet_ledger__wallet ON merchant.franchise_wallet_ledger(wallet_id, occurred_at DESC);

\echo '✓ 0023 merchant done.'
