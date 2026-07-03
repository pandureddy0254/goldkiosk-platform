-- ============================================================================
-- 0010_tables_crm_core.sql
-- Schema: crm
-- Tables: profiles, leads, lead_activities, partners, tenants, contracts,
--         activation_keys, user_preferences
-- Idempotent.
-- ============================================================================

\echo '-- 0010 crm core tables --'

-- --- profiles (internal CRM staff) ------------------------------------------
-- Self-contained: no auth.users FK (this port has no Supabase auth schema).
-- In production, authentication is external (Entra ID / app-managed); the
-- password_hash column is optional and only populated by the demo seed.
CREATE TABLE IF NOT EXISTS crm.profiles (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    full_name         text         NOT NULL,
    email             citext       NOT NULL,
    role              text         NOT NULL DEFAULT 'sales_rep',
    avatar_url        text         NULL,
    password_hash     text         NULL,
    last_signed_in_at timestamptz  NULL,
    created_at        timestamptz  NOT NULL DEFAULT now(),
    updated_at        timestamptz  NOT NULL DEFAULT now(),
    CONSTRAINT uq_profiles__email UNIQUE (email),
    CONSTRAINT ck_profiles__role CHECK (role IN ('sales_manager','sales_rep','superadmin'))
);
COMMENT ON TABLE crm.profiles IS 'Internal CRM staff. role in {sales_manager, sales_rep, superadmin}. Referenced by every actor/owner column.';

-- --- leads ------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS crm.leads (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    company_name          text            NOT NULL,
    contact_name          text            NOT NULL,
    contact_email         citext          NULL,
    contact_phone         text            NULL,
    country               text            NULL,
    region                text            NULL,
    source                text            NOT NULL,
    stage                 text            NOT NULL DEFAULT 'new',
    estimated_value       numeric(14,2)   NULL,
    currency_code         text            NOT NULL DEFAULT 'USD',
    estimated_kiosk_count integer         NULL,
    owner_id              uuid            NULL,
    notes                 text            NULL,
    lost_reason           text            NULL,
    -- proposal content (single proposal per lead for v1)
    scope_summary         text            NULL,
    deliverables          text            NULL,
    payment_terms         text            NULL,
    validity_days         integer         NOT NULL DEFAULT 30,
    deleted_at            timestamptz     NULL,
    created_at            timestamptz     NOT NULL DEFAULT now(),
    updated_at            timestamptz     NOT NULL DEFAULT now(),
    CONSTRAINT fk_leads__owner FOREIGN KEY (owner_id) REFERENCES crm.profiles(id) ON DELETE RESTRICT,
    CONSTRAINT ck_leads__currency_supported CHECK (currency_code IN ('USD','AED','INR','EUR','GBP','SAR','QAR','KWD','BHD','OMR')),
    CONSTRAINT ck_leads__validity_days CHECK (validity_days BETWEEN 1 AND 365),
    CONSTRAINT ck_leads__source CHECK (source IN ('website','inbound_call','partner_referral','conference','outbound')),
    CONSTRAINT ck_leads__stage CHECK (stage IN ('new','contacted','qualified','proposal','won','lost'))
);
CREATE INDEX IF NOT EXISTS ix_leads__stage      ON crm.leads (stage)      WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_leads__owner      ON crm.leads (owner_id)   WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_leads__source     ON crm.leads (source)     WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_leads__country    ON crm.leads (country)    WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_leads__created_at ON crm.leads (created_at DESC);
COMMENT ON TABLE crm.leads IS 'Inbound enquiries managed by CRM staff. Soft-delete via deleted_at.';

-- --- lead_activities --------------------------------------------------------
CREATE TABLE IF NOT EXISTS crm.lead_activities (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    lead_id       uuid              NOT NULL,
    actor_id      uuid              NULL,
    activity_type text              NOT NULL,
    payload       jsonb             NOT NULL DEFAULT '{}'::jsonb,
    occurred_at   timestamptz       NOT NULL DEFAULT now(),
    CONSTRAINT fk_lead_activities__leads FOREIGN KEY (lead_id)  REFERENCES crm.leads(id)    ON DELETE CASCADE,
    CONSTRAINT fk_lead_activities__actor FOREIGN KEY (actor_id) REFERENCES crm.profiles(id) ON DELETE SET NULL,
    CONSTRAINT ck_lead_activities__activity_type CHECK (activity_type IN ('note','call','email','stage_change','proposal_sent','won','lost'))
);
CREATE INDEX IF NOT EXISTS ix_lead_activities__lead_occurred ON crm.lead_activities (lead_id, occurred_at DESC);
COMMENT ON TABLE crm.lead_activities IS 'Append-mostly timeline of touchpoints on a lead.';

-- --- partners ---------------------------------------------------------------
CREATE TABLE IF NOT EXISTS crm.partners (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    legal_name               text             NOT NULL,
    display_name             text             NOT NULL,
    billing_address          jsonb            NOT NULL DEFAULT '{}'::jsonb,
    region                   text             NULL,
    primary_admin_name       text             NOT NULL,
    primary_admin_email      citext           NOT NULL,
    primary_admin_role_title text             NULL,
    initial_kiosk_count      integer          NOT NULL DEFAULT 0,
    mrr_amount               numeric(14,2)    NOT NULL DEFAULT 0,
    currency_code            text             NOT NULL DEFAULT 'USD',
    tenant_status            text             NOT NULL DEFAULT 'provisioning',
    provisioned_at           timestamptz      NULL,
    churned_at               timestamptz      NULL,
    lead_id                  uuid             NULL,
    created_at               timestamptz      NOT NULL DEFAULT now(),
    updated_at               timestamptz      NOT NULL DEFAULT now(),
    CONSTRAINT fk_partners__leads FOREIGN KEY (lead_id) REFERENCES crm.leads(id) ON DELETE SET NULL,
    CONSTRAINT ck_partners__currency_supported CHECK (currency_code IN ('USD','AED','INR','EUR','GBP','SAR','QAR','KWD','BHD','OMR')),
    CONSTRAINT ck_partners__tenant_status CHECK (tenant_status IN ('provisioning','active','suspended','churned'))
);
CREATE INDEX IF NOT EXISTS ix_partners__status     ON crm.partners (tenant_status);
CREATE INDEX IF NOT EXISTS ix_partners__region     ON crm.partners (region);
CREATE INDEX IF NOT EXISTS ix_partners__legal_name ON crm.partners (legal_name text_pattern_ops);
CREATE INDEX IF NOT EXISTS ix_partners__lead       ON crm.partners (lead_id);
COMMENT ON TABLE crm.partners IS 'Onboarded customer organisations. A row appears when a lead is marked won.';

-- --- tenants (1:1 with partner; cross-product link to the Admin Dashboard) --
CREATE TABLE IF NOT EXISTS crm.tenants (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    partner_id          uuid                    NOT NULL,
    admin_tenant_id     uuid                    NULL,   -- UUID in the Admin Dashboard's own DB
    region_code         text                    NOT NULL,
    provisioning_status text                    NOT NULL DEFAULT 'pending',
    provisioning_error  text                    NULL,
    created_at          timestamptz             NOT NULL DEFAULT now(),
    updated_at          timestamptz             NOT NULL DEFAULT now(),
    CONSTRAINT uq_tenants__partner UNIQUE (partner_id),
    CONSTRAINT fk_tenants__partners FOREIGN KEY (partner_id) REFERENCES crm.partners(id) ON DELETE CASCADE,
    CONSTRAINT ck_tenants__provisioning_status CHECK (provisioning_status IN ('pending','in_progress','succeeded','failed'))
);
CREATE INDEX IF NOT EXISTS ix_tenants__status ON crm.tenants (provisioning_status);
COMMENT ON TABLE crm.tenants IS '1:1 with partners. admin_tenant_id is the FK into the Admin Dashboard product DB.';

-- --- contracts --------------------------------------------------------------
CREATE TABLE IF NOT EXISTS crm.contracts (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    partner_id      uuid                NOT NULL,
    contract_type   text                NOT NULL,
    status          text                NOT NULL DEFAULT 'draft',
    signed_at       timestamptz         NULL,
    term_months     integer             NULL,
    value_amount    numeric(14,2)       NULL,
    currency_code   text                NOT NULL DEFAULT 'USD',
    document_url    text                NULL,
    signed_by_name  text                NULL,
    signed_by_email citext              NULL,
    created_at      timestamptz         NOT NULL DEFAULT now(),
    updated_at      timestamptz         NOT NULL DEFAULT now(),
    CONSTRAINT fk_contracts__partners FOREIGN KEY (partner_id) REFERENCES crm.partners(id) ON DELETE CASCADE,
    CONSTRAINT ck_contracts__currency_supported CHECK (currency_code IN ('USD','AED','INR','EUR','GBP','SAR','QAR','KWD','BHD','OMR')),
    CONSTRAINT ck_contracts__contract_type CHECK (contract_type IN ('msa','sow','dpa','sla','nda','addendum')),
    CONSTRAINT ck_contracts__status CHECK (status IN ('draft','out_for_signature','signed','terminated'))
);
CREATE INDEX IF NOT EXISTS ix_contracts__partner ON crm.contracts (partner_id);
CREATE INDEX IF NOT EXISTS ix_contracts__status  ON crm.contracts (status);
COMMENT ON TABLE crm.contracts IS 'Per-partner contract registry (MSA/SOW/DPA/SLA/NDA/addenda).';

-- --- activation_keys (SHA-256 hash only; cleartext never persisted) ---------
CREATE TABLE IF NOT EXISTS crm.activation_keys (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid        NOT NULL,
    key_prefix      text        NOT NULL,   -- e.g. 'AIKI-7F2A' - safe to display
    key_hash        text        NOT NULL,   -- sha256 hex of full cleartext key
    issued_to_email citext      NOT NULL,
    issued_at       timestamptz NOT NULL DEFAULT now(),
    expires_at      timestamptz NOT NULL DEFAULT (now() + interval '30 days'),
    consumed_at     timestamptz NULL,
    revoked_at      timestamptz NULL,
    revoked_reason  text        NULL,
    created_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_activation_keys__tenants FOREIGN KEY (tenant_id) REFERENCES crm.tenants(id) ON DELETE CASCADE
);
-- Only one *live* (neither consumed nor revoked) key per tenant.
CREATE UNIQUE INDEX IF NOT EXISTS ux_activation_keys__one_live_per_tenant
  ON crm.activation_keys (tenant_id) WHERE consumed_at IS NULL AND revoked_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_activation_keys__tenant  ON crm.activation_keys (tenant_id);
CREATE INDEX IF NOT EXISTS ix_activation_keys__expires ON crm.activation_keys (expires_at);
COMMENT ON TABLE crm.activation_keys IS 'SHA-256 hashes of activation keys. Cleartext is unrecoverable. One live key per tenant.';

-- --- user_preferences (per-staff UI prefs; 1:1 with profiles) ---------------
CREATE TABLE IF NOT EXISTS crm.user_preferences (
    user_id    uuid PRIMARY KEY,
    theme      text        NOT NULL DEFAULT 'light',
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_user_preferences__profiles FOREIGN KEY (user_id) REFERENCES crm.profiles(id) ON DELETE CASCADE,
    CONSTRAINT ck_user_preferences__theme CHECK (theme IN ('light','dark'))
);
COMMENT ON TABLE crm.user_preferences IS 'Per-user UI preferences (theme). One row per profile.';

\echo 'ok: 0010 crm core tables done.'
