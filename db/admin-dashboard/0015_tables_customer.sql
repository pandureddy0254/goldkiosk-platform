-- ============================================================================
-- 0015_tables_customer.sql
-- Schema: customer
-- Tables: customers, customer_profiles, customer_identifiers, customer_addresses,
--         customer_contacts, kyc_verifications, kyc_documents, kyc_reviews,
--         consent_records, customer_wallets, wallet_ledger_entries
-- ============================================================================

\echo '── 0015 customer ──'

-- ─── customers ──────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS customer.customers (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id             uuid NOT NULL,
    customer_code         citext NOT NULL,
    given_name_enc        bytea NOT NULL,
    family_name_enc       bytea NOT NULL,
    date_of_birth_enc     bytea NULL,
    gender                text NULL,
    primary_nationality   public.domain_country_code NULL,
    mobile_lookup_hash    bytea NULL,
    status                text NOT NULL DEFAULT 'prospect',
    created_at            timestamptz NOT NULL DEFAULT now(),
    updated_at            timestamptz NOT NULL DEFAULT now(),
    deleted_at            timestamptz NULL,
    CONSTRAINT fk_customers__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_customers__tenant_code UNIQUE (tenant_id, customer_code),
    CONSTRAINT ck_customers__gender CHECK (gender IS NULL OR gender IN ('male','female','other','undisclosed')),
    CONSTRAINT ck_customers__status CHECK (status IN ('prospect','active','blocked','archived'))
);

CREATE INDEX IF NOT EXISTS ix_customers__tenant ON customer.customers(tenant_id) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_customers__mobile_lookup ON customer.customers(tenant_id, mobile_lookup_hash) WHERE mobile_lookup_hash IS NOT NULL;

-- ─── customer_profiles ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS customer.customer_profiles (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id         uuid NOT NULL,
    occupation          text NULL,
    source_of_funds     text NULL,
    preferred_language  public.domain_locale_tag NULL,
    marketing_opt_in    boolean NOT NULL DEFAULT false,
    risk_band           text NULL,
    CONSTRAINT fk_customer_profiles__customers FOREIGN KEY (customer_id) REFERENCES customer.customers(id) ON DELETE CASCADE,
    CONSTRAINT uq_customer_profiles__customer UNIQUE (customer_id),
    CONSTRAINT ck_customer_profiles__risk CHECK (risk_band IS NULL OR risk_band IN ('low','medium','high'))
);

-- ─── customer_identifiers ───────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS customer.customer_identifiers (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id             uuid NOT NULL,
    id_type                 text NOT NULL,
    id_number_enc           bytea NOT NULL,
    id_number_lookup_hash   bytea NOT NULL,
    issuing_country         public.domain_country_code NOT NULL,
    issued_on               date NULL,
    expires_on              date NULL,
    is_primary              boolean NOT NULL DEFAULT false,
    created_at              timestamptz NOT NULL DEFAULT now(),
    deleted_at              timestamptz NULL,
    CONSTRAINT fk_customer_identifiers__customers FOREIGN KEY (customer_id) REFERENCES customer.customers(id) ON DELETE CASCADE,
    CONSTRAINT fk_customer_identifiers__countries FOREIGN KEY (issuing_country) REFERENCES tenancy.countries(iso_code),
    CONSTRAINT ck_customer_identifiers__type CHECK (id_type IN ('passport','national_id','driver_license','residence_permit','other'))
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_customer_identifiers__lookup ON customer.customer_identifiers(issuing_country, id_number_lookup_hash) WHERE deleted_at IS NULL;

-- ─── customer_addresses ─────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS customer.customer_addresses (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id       uuid NOT NULL,
    label             text NULL,
    street_enc        bytea NOT NULL,
    city              text NOT NULL,
    state_or_region   text NULL,
    postal_code_enc   bytea NULL,
    country_code      public.domain_country_code NOT NULL,
    is_current        boolean NOT NULL DEFAULT true,
    valid_from        date NULL,
    valid_to          date NULL,
    CONSTRAINT fk_customer_addresses__customers FOREIGN KEY (customer_id) REFERENCES customer.customers(id) ON DELETE CASCADE,
    CONSTRAINT fk_customer_addresses__countries FOREIGN KEY (country_code) REFERENCES tenancy.countries(iso_code)
);

CREATE INDEX IF NOT EXISTS ix_customer_addresses__customer ON customer.customer_addresses(customer_id) WHERE is_current = true;

-- ─── customer_contacts ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS customer.customer_contacts (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id         uuid NOT NULL,
    channel             text NOT NULL,
    value_enc           bytea NOT NULL,
    value_lookup_hash   bytea NOT NULL,
    is_verified         boolean NOT NULL DEFAULT false,
    verified_at         timestamptz NULL,
    is_primary          boolean NOT NULL DEFAULT false,
    CONSTRAINT fk_customer_contacts__customers FOREIGN KEY (customer_id) REFERENCES customer.customers(id) ON DELETE CASCADE,
    CONSTRAINT ck_customer_contacts__channel CHECK (channel IN ('email','phone','sms'))
);

CREATE INDEX IF NOT EXISTS ix_customer_contacts__lookup ON customer.customer_contacts(channel, value_lookup_hash);

-- ─── kyc_verifications ──────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS customer.kyc_verifications (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id          uuid NOT NULL,
    level                text NOT NULL DEFAULT 'basic',
    provider             text NOT NULL,
    provider_reference   text NULL,
    decision             text NOT NULL DEFAULT 'pending',
    risk_score           numeric(5,2) NULL,
    risk_band            text NULL,
    started_at           timestamptz NOT NULL DEFAULT now(),
    decided_at           timestamptz NULL,
    expires_at           timestamptz NULL,
    raw_response_enc     bytea NULL,
    CONSTRAINT fk_kyc_verifications__customers FOREIGN KEY (customer_id) REFERENCES customer.customers(id) ON DELETE CASCADE,
    CONSTRAINT ck_kyc_verifications__level    CHECK (level    IN ('basic','enhanced','periodic')),
    CONSTRAINT ck_kyc_verifications__decision CHECK (decision IN ('pending','approved','rejected','review')),
    CONSTRAINT ck_kyc_verifications__risk     CHECK (risk_band IS NULL OR risk_band IN ('low','medium','high'))
);

CREATE INDEX IF NOT EXISTS ix_kyc_verifications__customer ON customer.kyc_verifications(customer_id, started_at DESC);

-- ─── kyc_documents ──────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS customer.kyc_documents (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    kyc_verification_id   uuid NOT NULL,
    document_type         text NOT NULL,
    blob_uri_enc          bytea NOT NULL,
    hash_sha256           bytea NOT NULL,
    mime_type             text NOT NULL,
    size_bytes            integer NOT NULL,
    captured_at           timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_kyc_documents__verifications FOREIGN KEY (kyc_verification_id) REFERENCES customer.kyc_verifications(id) ON DELETE CASCADE,
    CONSTRAINT ck_kyc_documents__type CHECK (document_type IN ('id_front','id_back','selfie','proof_of_address','signature','other'))
);

-- ─── kyc_reviews ────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS customer.kyc_reviews (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    kyc_verification_id  uuid NOT NULL,
    reviewer_user_id     uuid NULL,
    queued_at            timestamptz NOT NULL DEFAULT now(),
    assigned_at          timestamptz NULL,
    reviewed_at          timestamptz NULL,
    outcome              text NULL,
    notes_enc            bytea NULL,
    CONSTRAINT fk_kyc_reviews__verifications FOREIGN KEY (kyc_verification_id) REFERENCES customer.kyc_verifications(id) ON DELETE CASCADE,
    CONSTRAINT fk_kyc_reviews__users         FOREIGN KEY (reviewer_user_id) REFERENCES identity.users(id),
    CONSTRAINT uq_kyc_reviews__verification UNIQUE (kyc_verification_id),
    CONSTRAINT ck_kyc_reviews__outcome CHECK (outcome IS NULL OR outcome IN ('approve','reject','escalate'))
);

-- ─── consent_records ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS customer.consent_records (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id     uuid NOT NULL,
    consent_type    text NOT NULL,
    policy_version  text NOT NULL,
    granted         boolean NOT NULL,
    granted_at      timestamptz NOT NULL DEFAULT now(),
    kiosk_id        uuid NULL,
    transaction_id  uuid NULL,
    proof_blob_uri  text NULL,
    CONSTRAINT fk_consent_records__customers FOREIGN KEY (customer_id) REFERENCES customer.customers(id) ON DELETE CASCADE,
    CONSTRAINT fk_consent_records__kiosks    FOREIGN KEY (kiosk_id)    REFERENCES kiosk.kiosks(id)
);

CREATE INDEX IF NOT EXISTS ix_consent_records__customer ON customer.consent_records(customer_id);

-- ─── customer_wallets ───────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS customer.customer_wallets (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id       uuid NOT NULL,
    currency_code     public.domain_currency_code NOT NULL,
    balance           public.domain_money NOT NULL DEFAULT 0,
    pin_hash          text NOT NULL,
    status            text NOT NULL DEFAULT 'active',
    last_updated_at   timestamptz NOT NULL DEFAULT now(),
    created_at        timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_customer_wallets__customers  FOREIGN KEY (customer_id)   REFERENCES customer.customers(id) ON DELETE CASCADE,
    CONSTRAINT fk_customer_wallets__currencies FOREIGN KEY (currency_code) REFERENCES tenancy.currencies(code),
    CONSTRAINT uq_customer_wallets__customer_currency UNIQUE (customer_id, currency_code),
    CONSTRAINT ck_customer_wallets__status CHECK (status IN ('active','frozen','closed'))
);

-- ─── wallet_ledger_entries (append-only) ────────────────────────────────────
CREATE TABLE IF NOT EXISTS customer.wallet_ledger_entries (
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
    CONSTRAINT pk_wallet_ledger_entries PRIMARY KEY (sequence_no),
    CONSTRAINT uq_wallet_ledger_entries__id UNIQUE (id),
    CONSTRAINT fk_wallet_ledger_entries__wallets FOREIGN KEY (wallet_id) REFERENCES customer.customer_wallets(id) ON DELETE CASCADE,
    CONSTRAINT fk_wallet_ledger_entries__users   FOREIGN KEY (posted_by_user_id) REFERENCES identity.users(id),
    CONSTRAINT ck_wallet_ledger_entries__kind CHECK (entry_kind IN ('topup','debit','payout','redemption','adjustment','refund'))
);

CREATE INDEX IF NOT EXISTS ix_wallet_ledger_entries__wallet ON customer.wallet_ledger_entries(wallet_id, occurred_at DESC);

\echo '✓ 0015 customer done.'
