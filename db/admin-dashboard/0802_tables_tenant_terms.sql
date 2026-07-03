-- ============================================================================
-- 0802_tables_tenant_terms.sql
-- Schema: kiosk
-- Per-tenant Terms & Conditions content served to the KioskApp.
-- Sections stored as JSONB array: [{"heading":"...", "body":"..."}]
-- Idempotent.
-- ============================================================================

\echo '── 0802 tenant_terms ──'

CREATE TABLE IF NOT EXISTS kiosk.tenant_terms (
    id              uuid    PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       uuid    NOT NULL,
    version         text    NOT NULL,
    title           text    NOT NULL,
    effective_date  date    NOT NULL,
    sections_json   jsonb   NOT NULL DEFAULT '[]',
    is_active       boolean NOT NULL DEFAULT true,
    created_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_tenant_terms__tenants FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_tenant_terms__tenant_version UNIQUE (tenant_id, version)
);

CREATE INDEX IF NOT EXISTS ix_tenant_terms__tenant
    ON kiosk.tenant_terms(tenant_id) WHERE is_active = true;

-- ─── Seed: Xiphias Gold Company T&C ─────────────────────────────────────────
INSERT INTO kiosk.tenant_terms (tenant_id, version, title, effective_date, sections_json)
SELECT t.id,
       'v1.0',
       'Seller Agreement',
       '2026-06-23',
       '[
         {
           "heading": "1. Ownership",
           "body": "The Seller declares and warrants that the property being sold is not stolen, rented or leased and that they have no liens or encumbrances against them. The Seller attests to having good title to the property and the right to sell it."
         },
         {
           "heading": "2. Identity Verification",
           "body": "The Seller consents to Aadhaar-based eKYC, PAN verification, and capture of a photograph for compliance with applicable Indian regulations."
         },
         {
           "heading": "3. Valuation",
           "body": "The offer is based on measured weight and metal purity at the current market rate, less a standard processing margin. The Seller accepts the offer as final before payout is initiated."
         },
         {
           "heading": "4. Payout",
           "body": "Funds are remitted digitally via UPI or bank transfer to the account provided by the Seller. Xiphias Gold Company is not liable for delays caused by the Seller providing incorrect account details."
         },
         {
           "heading": "5. Data & Privacy",
           "body": "Personal information is collected solely for KYC compliance, encrypted at rest, and deleted after the applicable statutory retention period. It will not be shared with third parties except as required by law."
         }
       ]'::jsonb
FROM tenancy.tenants t
WHERE t.code = 'XIPHIAS'
ON CONFLICT (tenant_id, version) DO NOTHING;

\echo '✓ 0802 tenant_terms done.'
