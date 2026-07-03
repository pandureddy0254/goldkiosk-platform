-- ============================================================================
-- 0017_tables_pricing.sql
-- Schema: pricing
-- Per-transaction price offers + market rate sources.
-- Tables: metal_rate_sources, metal_rates, pricing_policies, tier_deductions,
--         payout_matrix, fx_rate_snapshots, market_snapshots, offers, offer_lines
-- ============================================================================

\echo '── 0017 pricing ──'

-- ─── metal_rate_sources ─────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS pricing.metal_rate_sources (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code                     citext NOT NULL,
    name                     text NOT NULL,
    adapter_type             text NOT NULL,
    poll_interval_seconds    integer NOT NULL DEFAULT 300,
    freshness_sla_seconds    integer NOT NULL DEFAULT 900,
    is_active                boolean NOT NULL DEFAULT true,
    CONSTRAINT uq_metal_rate_sources__code UNIQUE (code)
);

-- ─── metal_rates (append-only history) ──────────────────────────────────────
CREATE TABLE IF NOT EXISTS pricing.metal_rates (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    metal                 text NOT NULL,
    purity_karat          public.domain_karat NOT NULL,
    price_per_gram        public.domain_money NOT NULL,
    currency_code         public.domain_currency_code NOT NULL,
    metal_rate_source_id  uuid NOT NULL,
    retrieved_at          timestamptz NOT NULL DEFAULT now(),
    valid_until           timestamptz NULL,
    CONSTRAINT fk_metal_rates__sources    FOREIGN KEY (metal_rate_source_id) REFERENCES pricing.metal_rate_sources(id),
    CONSTRAINT fk_metal_rates__currencies FOREIGN KEY (currency_code)        REFERENCES tenancy.currencies(code),
    CONSTRAINT ck_metal_rates__metal CHECK (metal IN ('gold','silver','platinum','palladium'))
);

CREATE INDEX IF NOT EXISTS ix_metal_rates__live ON pricing.metal_rates(metal, purity_karat, currency_code, retrieved_at DESC);

-- ─── pricing_policies ───────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS pricing.pricing_policies (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL,
    code            citext NOT NULL,
    display_name    text NOT NULL,
    rate_source     text NOT NULL,
    spread_pct      public.domain_percent NOT NULL DEFAULT 0,
    fixed_fee       public.domain_money NOT NULL DEFAULT 0,
    currency_code   public.domain_currency_code NOT NULL,
    is_active       boolean NOT NULL DEFAULT true,
    effective_from  timestamptz NOT NULL DEFAULT now(),
    effective_to    timestamptz NULL,
    CONSTRAINT fk_pricing_policies__tenants    FOREIGN KEY (tenant_id)     REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_pricing_policies__currencies FOREIGN KEY (currency_code) REFERENCES tenancy.currencies(code),
    CONSTRAINT uq_pricing_policies__tenant_code UNIQUE (tenant_id, code)
);

-- ─── tier_deductions ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS pricing.tier_deductions (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    pricing_policy_id   uuid NOT NULL,
    karat               public.domain_karat NOT NULL,
    deduction_pct       public.domain_percent NOT NULL,
    effective_from      timestamptz NOT NULL DEFAULT now(),
    effective_to        timestamptz NULL,
    CONSTRAINT fk_tier_deductions__policies FOREIGN KEY (pricing_policy_id) REFERENCES pricing.pricing_policies(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_tier_deductions__policy_karat ON pricing.tier_deductions(pricing_policy_id, karat);

-- ─── payout_matrix ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS pricing.payout_matrix (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    pricing_policy_id   uuid NOT NULL,
    metal               text NOT NULL,
    karat               public.domain_karat NOT NULL,
    weight_g_bucket     public.domain_grams NOT NULL,
    precomputed_payout  public.domain_money NOT NULL,
    computed_at         timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_payout_matrix__policies FOREIGN KEY (pricing_policy_id) REFERENCES pricing.pricing_policies(id) ON DELETE CASCADE,
    CONSTRAINT ck_payout_matrix__metal CHECK (metal IN ('gold','silver','platinum','palladium'))
);

-- ─── fx_rate_snapshots (append-only) ────────────────────────────────────────
CREATE TABLE IF NOT EXISTS pricing.fx_rate_snapshots (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    captured_at   timestamptz NOT NULL DEFAULT now(),
    base_ccy      public.domain_currency_code NOT NULL,
    quote_ccy     public.domain_currency_code NOT NULL,
    rate          numeric(18,8) NOT NULL,
    source        text NOT NULL,
    CONSTRAINT ck_fx_rate_snapshots__different CHECK (base_ccy <> quote_ccy)
);

CREATE INDEX IF NOT EXISTS ix_fx_rate_snapshots__pair_time ON pricing.fx_rate_snapshots(base_ccy, quote_ccy, captured_at DESC);

-- ─── market_snapshots ───────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS pricing.market_snapshots (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    snapshot_at   timestamptz NOT NULL DEFAULT now(),
    rates_json    jsonb NOT NULL
);

-- ─── offers (per-transaction price quote) ───────────────────────────────────
CREATE TABLE IF NOT EXISTS pricing.offers (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id       uuid NOT NULL,
    pricing_policy_id    uuid NOT NULL,
    status               text NOT NULL DEFAULT 'draft',
    total_offered        public.domain_money NOT NULL DEFAULT 0,
    currency_code        public.domain_currency_code NOT NULL,
    presented_at         timestamptz NULL,
    expires_at           timestamptz NULL,
    accepted_at          timestamptz NULL,
    declined_at          timestamptz NULL,
    CONSTRAINT fk_offers__transactions FOREIGN KEY (transaction_id)    REFERENCES tx.transactions(id) ON DELETE CASCADE,
    CONSTRAINT fk_offers__policies     FOREIGN KEY (pricing_policy_id) REFERENCES pricing.pricing_policies(id),
    CONSTRAINT fk_offers__currencies   FOREIGN KEY (currency_code)     REFERENCES tenancy.currencies(code),
    CONSTRAINT uq_offers__transaction  UNIQUE (transaction_id),
    CONSTRAINT ck_offers__status CHECK (status IN ('draft','presented','accepted','declined','expired'))
);

-- ─── offer_lines ────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS pricing.offer_lines (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    offer_id            uuid NOT NULL,
    transaction_item_id uuid NOT NULL,
    metal_rate_id       uuid NOT NULL,
    tier_deduction_id   uuid NULL,
    billed_karat        public.domain_karat NOT NULL,
    billed_weight_g     public.domain_grams NOT NULL,
    rate_per_gram       public.domain_money NOT NULL,
    gross_value         public.domain_money NOT NULL,
    deductions          public.domain_money NOT NULL DEFAULT 0,
    net_offered         public.domain_money NOT NULL,
    CONSTRAINT fk_offer_lines__offers       FOREIGN KEY (offer_id)            REFERENCES pricing.offers(id) ON DELETE CASCADE,
    CONSTRAINT fk_offer_lines__items        FOREIGN KEY (transaction_item_id) REFERENCES tx.transaction_items(id),
    CONSTRAINT fk_offer_lines__metal_rates  FOREIGN KEY (metal_rate_id)       REFERENCES pricing.metal_rates(id),
    CONSTRAINT fk_offer_lines__deductions   FOREIGN KEY (tier_deduction_id)   REFERENCES pricing.tier_deductions(id)
);

CREATE INDEX IF NOT EXISTS ix_offer_lines__offer ON pricing.offer_lines(offer_id);

\echo '✓ 0017 pricing done.'
