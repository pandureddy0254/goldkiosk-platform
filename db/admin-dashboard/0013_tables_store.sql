-- ============================================================================
-- 0013_tables_store.sql
-- Schema: store
-- Physical stores hosting kiosks.
-- Tables: stores, store_locations, store_contacts, store_balances, store_operators
-- ============================================================================

\echo '── 0013 store ──'

-- ─── stores ─────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS store.stores (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id     uuid NOT NULL,
    code          citext NOT NULL,
    display_name  text NOT NULL,
    status        text NOT NULL DEFAULT 'onboarding',
    time_zone     text NOT NULL DEFAULT 'UTC',
    opened_at     timestamptz NULL,
    closed_at     timestamptz NULL,
    created_at    timestamptz NOT NULL DEFAULT now(),
    updated_at    timestamptz NOT NULL DEFAULT now(),
    deleted_at    timestamptz NULL,
    CONSTRAINT fk_stores__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_stores__tenant_code UNIQUE (tenant_id, code),
    CONSTRAINT ck_stores__status CHECK (status IN ('onboarding','active','suspended','closed'))
);

CREATE INDEX IF NOT EXISTS ix_stores__tenant ON store.stores(tenant_id) WHERE deleted_at IS NULL;

-- ─── store_locations ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS store.store_locations (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    store_id           uuid NOT NULL,
    address_line1_enc  bytea NOT NULL,
    address_line2_enc  bytea NULL,
    city               text NOT NULL,
    state_or_region    text NULL,
    postal_code_enc    bytea NULL,
    country_code       public.domain_country_code NOT NULL,
    latitude           numeric(9,6) NULL,
    longitude          numeric(9,6) NULL,
    CONSTRAINT fk_store_locations__stores    FOREIGN KEY (store_id)     REFERENCES store.stores(id) ON DELETE CASCADE,
    CONSTRAINT fk_store_locations__countries FOREIGN KEY (country_code) REFERENCES tenancy.countries(iso_code),
    CONSTRAINT uq_store_locations__store    UNIQUE (store_id)
);

-- ─── store_contacts ─────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS store.store_contacts (
    id          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    store_id    uuid NOT NULL,
    role        text NOT NULL,
    name        text NOT NULL,
    email_enc   bytea NULL,
    phone_enc   bytea NULL,
    is_primary  boolean NOT NULL DEFAULT false,
    deleted_at  timestamptz NULL,
    CONSTRAINT fk_store_contacts__stores FOREIGN KEY (store_id) REFERENCES store.stores(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_store_contacts__store ON store.store_contacts(store_id) WHERE deleted_at IS NULL;

-- ─── store_balances ─────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS store.store_balances (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    store_id              uuid NOT NULL,
    currency_code         public.domain_currency_code NOT NULL,
    cash_on_hand          public.domain_money NOT NULL DEFAULT 0,
    metal_grams_gold      public.domain_grams NOT NULL DEFAULT 0,
    metal_grams_silver    public.domain_grams NOT NULL DEFAULT 0,
    metal_grams_platinum  public.domain_grams NOT NULL DEFAULT 0,
    reconciled_at         timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_store_balances__stores     FOREIGN KEY (store_id)      REFERENCES store.stores(id) ON DELETE CASCADE,
    CONSTRAINT fk_store_balances__currencies FOREIGN KEY (currency_code) REFERENCES tenancy.currencies(code),
    CONSTRAINT uq_store_balances__store_currency UNIQUE (store_id, currency_code)
);

-- ─── store_operators ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS store.store_operators (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    store_id     uuid NOT NULL,
    user_id      uuid NOT NULL,
    role         text NOT NULL,
    assigned_at  timestamptz NOT NULL DEFAULT now(),
    revoked_at   timestamptz NULL,
    CONSTRAINT fk_store_operators__stores FOREIGN KEY (store_id) REFERENCES store.stores(id) ON DELETE CASCADE,
    CONSTRAINT fk_store_operators__users  FOREIGN KEY (user_id)  REFERENCES identity.users(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_store_operators__user ON store.store_operators(user_id) WHERE revoked_at IS NULL;

\echo '✓ 0013 store done.'
