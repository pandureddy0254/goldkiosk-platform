-- ============================================================================
-- 0805_alter_terms_global_defaults.sql
-- Schema: kiosk
-- Makes tenant_id nullable on tenant_terms so global default T&C can exist.
-- tenant_id = NULL  → global default (shown to any tenant with no custom terms)
-- tenant_id = <id>  → tenant-specific (takes priority over global defaults)
-- Idempotent.
-- ============================================================================

\echo '── 0805 terms global defaults ──'

-- Allow NULL tenant_id (was NOT NULL + FK to tenancy.tenants)
ALTER TABLE kiosk.tenant_terms
    ALTER COLUMN tenant_id DROP NOT NULL,
    ALTER COLUMN tenant_id DROP DEFAULT;

-- Drop FK constraint if it exists (NULL rows have no tenant to reference)
DO $$
DECLARE
    _con text;
BEGIN
    SELECT conname INTO _con
    FROM pg_constraint
    WHERE conrelid = 'kiosk.tenant_terms'::regclass
      AND contype = 'f'
      AND conname ILIKE '%tenant%';
    IF _con IS NOT NULL THEN
        EXECUTE format('ALTER TABLE kiosk.tenant_terms DROP CONSTRAINT %I', _con);
    END IF;
END $$;

-- Recreate unique index allowing NULLs (same pattern as screensavers)
ALTER TABLE kiosk.tenant_terms DROP CONSTRAINT IF EXISTS uq_tenant_terms__tenant_version;
DROP INDEX IF EXISTS kiosk.uq_tenant_terms__tenant_version;

CREATE UNIQUE INDEX IF NOT EXISTS uq_tenant_terms__tenant_version
    ON kiosk.tenant_terms (tenant_id, version)
    WHERE tenant_id IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS uq_tenant_terms__global_version
    ON kiosk.tenant_terms (version)
    WHERE tenant_id IS NULL;

-- Seed global default Terms & Conditions (idempotent)
INSERT INTO kiosk.tenant_terms (id, tenant_id, version, title, effective_date, sections_json, is_active, created_at)
VALUES (
    gen_random_uuid(),
    NULL,
    '1.0',
    'Terms & Conditions',
    '2026-01-01',
    '[
        {"heading": "Service Overview", "body": "GoldKiosk provides an automated, self-service gold and silver buying experience. By using this kiosk, you agree to these terms."},
        {"heading": "Item Evaluation", "body": "Items are evaluated using XRF (X-ray fluorescence) analysis to determine metal purity and weighed on a certified scale. Results are final and binding."},
        {"heading": "Offer & Payout", "body": "The offer shown is based on live market rates and measured purity. You may accept the offer or have your item returned at no cost. Payouts are processed digitally (UPI or bank transfer) within 1 business day."},
        {"heading": "KYC & Privacy", "body": "Government-issued ID and biometric data are collected for regulatory compliance under India KYC norms. Your data is encrypted and never shared with third parties without your consent."},
        {"heading": "Dispute Resolution", "body": "For disputes, contact the kiosk operator or reach GoldKiosk support at support@goldkiosk.in. Disputes must be raised within 7 days of the transaction."}
    ]'::jsonb,
    true,
    now()
)
ON CONFLICT DO NOTHING;

\echo '── 0805 done ──'
