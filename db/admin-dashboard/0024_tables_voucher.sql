-- ============================================================================
-- 0024_tables_voucher.sql
-- Schema: voucher
-- Tables: vouchers, redemption_policies, voucher_redemptions, promotional_offers
-- ============================================================================

\echo '── 0024 voucher ──'

-- ─── vouchers ───────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS voucher.vouchers (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL,
    code            citext NOT NULL,
    name            text NOT NULL,
    description     text NULL,
    discount_type   text NOT NULL,
    discount_value  public.domain_money NOT NULL,
    currency_code   public.domain_currency_code NULL,
    validity_start  timestamptz NOT NULL,
    validity_end    timestamptz NOT NULL,
    usage_limit     integer NULL,
    used_count      integer NOT NULL DEFAULT 0,
    is_active       boolean NOT NULL DEFAULT true,
    created_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_vouchers__tenants    FOREIGN KEY (tenant_id)     REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_vouchers__currencies FOREIGN KEY (currency_code) REFERENCES tenancy.currencies(code),
    CONSTRAINT uq_vouchers__tenant_code UNIQUE (tenant_id, code),
    CONSTRAINT ck_vouchers__discount_type CHECK (discount_type IN ('percent','fixed')),
    CONSTRAINT ck_vouchers__validity      CHECK (validity_end >= validity_start)
);

-- ─── redemption_policies ────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS voucher.redemption_policies (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    voucher_id             uuid NOT NULL,
    min_purchase_amount    public.domain_money NULL,
    max_discount_cap       public.domain_money NULL,
    per_customer_limit     integer NULL,
    applicable_categories  jsonb NULL,
    effective_from         timestamptz NOT NULL DEFAULT now(),
    effective_to           timestamptz NULL,
    CONSTRAINT fk_redemption_policies__vouchers FOREIGN KEY (voucher_id) REFERENCES voucher.vouchers(id) ON DELETE CASCADE
);

-- ─── voucher_redemptions ────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS voucher.voucher_redemptions (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    voucher_id          uuid NOT NULL,
    customer_id         uuid NOT NULL,
    transaction_id      uuid NOT NULL,
    kiosk_id            uuid NOT NULL,
    redeemed_amount     public.domain_money NOT NULL,
    remaining_balance   public.domain_money NULL,
    redeemed_at         timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_voucher_redemptions__vouchers     FOREIGN KEY (voucher_id)     REFERENCES voucher.vouchers(id),
    CONSTRAINT fk_voucher_redemptions__customers    FOREIGN KEY (customer_id)    REFERENCES customer.customers(id),
    CONSTRAINT fk_voucher_redemptions__transactions FOREIGN KEY (transaction_id) REFERENCES tx.transactions(id) ON DELETE CASCADE,
    CONSTRAINT fk_voucher_redemptions__kiosks       FOREIGN KEY (kiosk_id)       REFERENCES kiosk.kiosks(id)
);

CREATE INDEX IF NOT EXISTS ix_voucher_redemptions__voucher  ON voucher.voucher_redemptions(voucher_id, redeemed_at DESC);
CREATE INDEX IF NOT EXISTS ix_voucher_redemptions__customer ON voucher.voucher_redemptions(customer_id);

-- ─── promotional_offers (campaigns, distinct from per-tx pricing.offers) ───
CREATE TABLE IF NOT EXISTS voucher.promotional_offers (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id            uuid NOT NULL,
    code                 citext NOT NULL,
    description          text NOT NULL,
    discount_pct         public.domain_percent NULL,
    validity_start       timestamptz NOT NULL,
    validity_end         timestamptz NOT NULL,
    is_active            boolean NOT NULL DEFAULT true,
    created_by_user_id   uuid NULL,
    created_at           timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_promotional_offers__tenants FOREIGN KEY (tenant_id)         REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_promotional_offers__users   FOREIGN KEY (created_by_user_id) REFERENCES identity.users(id),
    CONSTRAINT uq_promotional_offers__tenant_code UNIQUE (tenant_id, code),
    CONSTRAINT ck_promotional_offers__validity CHECK (validity_end >= validity_start)
);

\echo '✓ 0024 voucher done.'
