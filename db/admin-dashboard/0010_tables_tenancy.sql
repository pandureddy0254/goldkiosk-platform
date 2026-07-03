-- ============================================================================
-- 0010_tables_tenancy.sql
-- Schema: tenancy
-- Root of multi-tenancy + global reference data.
-- Tables: tenants, tenant_features, tenant_branding, tenant_configs,
--         legal_entities, countries, currencies, languages, regions
-- Idempotent.
-- ============================================================================

\echo '── 0010 tenancy ──'

-- ─── regions (global reference) ─────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tenancy.regions (
    code               text PRIMARY KEY,
    name               text NOT NULL,
    azure_region       text NOT NULL,
    regulatory_frame   text NULL,
    CONSTRAINT ck_regions__code_format CHECK (code ~ '^[A-Z]{3,10}$')
);

-- ─── countries (global reference) ───────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tenancy.countries (
    iso_code              public.domain_country_code PRIMARY KEY,
    name                  text NOT NULL,
    calling_code          text NOT NULL,
    default_currency_code public.domain_currency_code NOT NULL,
    default_language_tag  public.domain_locale_tag    NOT NULL,
    region_code           text NOT NULL,
    is_supported          boolean NOT NULL DEFAULT true,
    CONSTRAINT fk_countries__regions FOREIGN KEY (region_code) REFERENCES tenancy.regions(code)
);

CREATE INDEX IF NOT EXISTS ix_countries__region_code ON tenancy.countries(region_code);

-- ─── currencies (global reference) ──────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tenancy.currencies (
    code           public.domain_currency_code PRIMARY KEY,
    name           text NOT NULL,
    minor_units    smallint NOT NULL,
    symbol         text NOT NULL,
    rounding_rule  text NOT NULL DEFAULT 'half_up',
    CONSTRAINT ck_currencies__rounding CHECK (rounding_rule IN ('half_up','half_even','down'))
);

-- ─── languages (global reference) ───────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tenancy.languages (
    tag           public.domain_locale_tag PRIMARY KEY,
    english_name  text NOT NULL,
    native_name   text NOT NULL,
    direction     text NOT NULL DEFAULT 'ltr',
    is_supported  boolean NOT NULL DEFAULT true,
    CONSTRAINT ck_languages__direction CHECK (direction IN ('ltr','rtl'))
);

-- ─── tenants (root) ─────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tenancy.tenants (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code                   citext NOT NULL,
    legal_name             text   NOT NULL,
    tier                   text   NOT NULL,
    home_country_code      public.domain_country_code NOT NULL,
    data_residency_region  text   NOT NULL,
    status                 text   NOT NULL DEFAULT 'onboarding',
    onboarded_at           timestamptz NULL,
    created_at             timestamptz NOT NULL DEFAULT now(),
    updated_at             timestamptz NOT NULL DEFAULT now(),
    deleted_at             timestamptz NULL,
    CONSTRAINT uq_tenants__code UNIQUE (code),
    CONSTRAINT ck_tenants__tier   CHECK (tier   IN ('standard','premium','bank')),
    CONSTRAINT ck_tenants__status CHECK (status IN ('onboarding','active','suspended','offboarded')),
    CONSTRAINT fk_tenants__countries FOREIGN KEY (home_country_code) REFERENCES tenancy.countries(iso_code)
);

CREATE INDEX IF NOT EXISTS ix_tenants__status ON tenancy.tenants(status) WHERE deleted_at IS NULL;

-- ─── tenant_features ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tenancy.tenant_features (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL,
    feature_code    text NOT NULL,
    is_enabled      boolean NOT NULL DEFAULT true,
    effective_from  timestamptz NULL,
    effective_to    timestamptz NULL,
    config_json     jsonb NULL,
    CONSTRAINT fk_tenant_features__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_tenant_features__tenant_feature UNIQUE (tenant_id, feature_code)
);

-- ─── tenant_branding ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tenancy.tenant_branding (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid NOT NULL,
    surface         text NOT NULL,
    store_id        uuid NULL,
    logo_blob_uri   text NULL,
    favicon_uri     text NULL,
    palette_json    jsonb NULL,
    typography_json jsonb NULL,
    css_theme       text  NULL,
    created_at      timestamptz NOT NULL DEFAULT now(),
    deleted_at      timestamptz NULL,
    CONSTRAINT fk_tenant_branding__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT ck_tenant_branding__surface CHECK (surface IN ('kiosk','admin','portal','receipt'))
);

CREATE INDEX IF NOT EXISTS ix_tenant_branding__tenant ON tenancy.tenant_branding(tenant_id);

-- ─── tenant_configs ─────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tenancy.tenant_configs (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                   uuid NOT NULL,
    default_language_tag        public.domain_locale_tag NOT NULL,
    default_currency_code       public.domain_currency_code NOT NULL,
    business_hours_json         jsonb NULL,
    kiosk_idle_timeout_seconds  integer NOT NULL DEFAULT 60,
    photo_retention_days        integer NOT NULL DEFAULT 365,
    pii_retention_days          integer NOT NULL DEFAULT 2555,  -- 7 years
    created_at                  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_tenant_configs__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_tenant_configs__tenant UNIQUE (tenant_id),
    CONSTRAINT fk_tenant_configs__languages  FOREIGN KEY (default_language_tag)  REFERENCES tenancy.languages(tag),
    CONSTRAINT fk_tenant_configs__currencies FOREIGN KEY (default_currency_code) REFERENCES tenancy.currencies(code)
);

-- ─── legal_entities ─────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS tenancy.legal_entities (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               uuid NOT NULL,
    country_code            public.domain_country_code NOT NULL,
    registered_name         text NOT NULL,
    registration_number     text NOT NULL,
    tax_number              text NULL,
    registered_address_enc  bytea NULL,
    created_at              timestamptz NOT NULL DEFAULT now(),
    deleted_at              timestamptz NULL,
    CONSTRAINT fk_legal_entities__tenants   FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_legal_entities__countries FOREIGN KEY (country_code) REFERENCES tenancy.countries(iso_code)
);

CREATE INDEX IF NOT EXISTS ix_legal_entities__tenant ON tenancy.legal_entities(tenant_id);

-- ─── Seed reference data ─────────────────────────────────────────────────────
INSERT INTO tenancy.regions (code, name, azure_region, regulatory_frame) VALUES
    ('APAC',    'Asia-Pacific',         'southeastasia', 'MAS / FSRA'),
    ('MIDEAST', 'Middle East',          'uaenorth',      'DFSA / FSRA'),
    ('EMEA',    'Europe, ME and Africa','westeurope',    'GDPR / FCA'),
    ('NORTHAM', 'North America',        'canadacentral', 'FINTRAC / FinCEN')
ON CONFLICT (code) DO NOTHING;

INSERT INTO tenancy.currencies (code, name, minor_units, symbol, rounding_rule) VALUES
    ('AED','UAE Dirham',          2, 'د.إ', 'half_up'),
    ('SGD','Singapore Dollar',    2, 'S$',  'half_even'),
    ('USD','US Dollar',           2, '$',   'half_up'),
    ('INR','Indian Rupee',        2, '₹',   'half_up'),
    ('GBP','Pound Sterling',      2, '£',   'half_up'),
    ('EUR','Euro',                2, '€',   'half_even'),
    ('CAD','Canadian Dollar',     2, 'C$',  'half_up'),
    ('TTD','Trinidad & Tobago Dollar', 2, 'TT$', 'half_up')
ON CONFLICT (code) DO NOTHING;

INSERT INTO tenancy.languages (tag, english_name, native_name, direction, is_supported) VALUES
    ('en',    'English',           'English',  'ltr', true),
    ('en-AE', 'English (UAE)',     'English',  'ltr', true),
    ('en-SG', 'English (Singapore)','English', 'ltr', true),
    ('en-IN', 'English (India)',   'English',  'ltr', true),
    ('en-GB', 'English (UK)',      'English',  'ltr', true),
    ('en-CA', 'English (Canada)',  'English',  'ltr', true),
    ('ar',    'Arabic',            'العربية',  'rtl', true),
    ('ar-AE', 'Arabic (UAE)',      'العربية',  'rtl', true),
    ('hi',    'Hindi',             'हिन्दी',   'ltr', true),
    ('zh-Hans','Chinese (Simplified)','简体中文','ltr', true)
ON CONFLICT (tag) DO NOTHING;

INSERT INTO tenancy.countries (iso_code, name, calling_code, default_currency_code, default_language_tag, region_code, is_supported) VALUES
    ('AE','United Arab Emirates','+971','AED','en-AE','MIDEAST', true),
    ('SG','Singapore',            '+65','SGD','en-SG','APAC',    true),
    ('IN','India',                '+91','INR','en-IN','APAC',    true),
    ('GB','United Kingdom',       '+44','GBP','en-GB','EMEA',    true),
    ('CA','Canada',                '+1','CAD','en-CA','NORTHAM', true),
    ('US','United States',         '+1','USD','en',   'NORTHAM', true),
    ('TT','Trinidad & Tobago',     '+1','TTD','en',   'NORTHAM', true)
ON CONFLICT (iso_code) DO NOTHING;

\echo '✓ 0010 tenancy done.'
