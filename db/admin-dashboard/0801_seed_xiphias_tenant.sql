-- ============================================================================
-- 0801_seed_xiphias_tenant.sql
-- Dev seed: Xiphias Gold Company tenant + store + GK01 kiosk link + screensavers.
-- Idempotent — safe to re-run.
-- ============================================================================

\echo '── 0801 seed xiphias tenant ──'

-- ─── Tenant ─────────────────────────────────────────────────────────────────
INSERT INTO tenancy.tenants (code, legal_name, tier, home_country_code,
    data_residency_region, status, onboarded_at)
VALUES ('XIPHIAS', 'Xiphias Gold Company', 'standard', 'IN', 'APAC', 'active', now())
ON CONFLICT (code) DO NOTHING;

-- ─── Store (required FK for kiosk.kiosks.store_id) ──────────────────────────
INSERT INTO store.stores (tenant_id, code, display_name, status, time_zone)
SELECT id, 'XGC-STORE-01', 'Xiphias Gold HQ', 'active', 'Asia/Kolkata'
FROM tenancy.tenants
WHERE code = 'XIPHIAS'
ON CONFLICT (tenant_id, code) DO NOTHING;

-- ─── Link GK01 kiosk to Xiphias tenant + store ───────────────────────────────
-- Updates the existing GK01 kiosk (created during Phase 2a setup).
-- PIN hash stays unchanged; only the tenant and store FK are set.
UPDATE kiosk.kiosks
SET tenant_id = (SELECT id FROM tenancy.tenants WHERE code = 'XIPHIAS'),
    store_id  = (SELECT s.id
                 FROM store.stores s
                 JOIN tenancy.tenants t ON t.id = s.tenant_id
                 WHERE t.code = 'XIPHIAS' AND s.code = 'XGC-STORE-01'),
    status    = 'active'
WHERE code = 'GK01';

-- ─── Screensaver slides for Xiphias ──────────────────────────────────────────
-- Placeholder images (placehold.co); replace with real brand assets later.
INSERT INTO kiosk.screen_savers (tenant_id, code, image_url, media_type, duration_seconds, display_order)
SELECT t.id, 'XGC-SS-01',
       'https://placehold.co/1920x1080/D7AC00/FFFFFF?text=Xiphias+Gold+Company',
       'image', 8, 1
FROM tenancy.tenants t WHERE t.code = 'XIPHIAS'
ON CONFLICT (tenant_id, code) DO NOTHING;

INSERT INTO kiosk.screen_savers (tenant_id, code, image_url, media_type, duration_seconds, display_order)
SELECT t.id, 'XGC-SS-02',
       'https://placehold.co/1920x1080/75521F/FFFFFF?text=Instant+Cash+for+Your+Gold',
       'image', 8, 2
FROM tenancy.tenants t WHERE t.code = 'XIPHIAS'
ON CONFLICT (tenant_id, code) DO NOTHING;

INSERT INTO kiosk.screen_savers (tenant_id, code, image_url, media_type, duration_seconds, display_order)
SELECT t.id, 'XGC-SS-03',
       'https://placehold.co/1920x1080/B8862F/FFFFFF?text=Safe+%7C+Instant+%7C+Trusted',
       'image', 8, 3
FROM tenancy.tenants t WHERE t.code = 'XIPHIAS'
ON CONFLICT (tenant_id, code) DO NOTHING;

-- ─── Kiosk languages for GK01 (6 India languages) ───────────────────────────
INSERT INTO kiosk.kiosk_languages (kiosk_id, language_id, display_order, is_active)
SELECT k.id, l.id, l.default_order, true
FROM kiosk.kiosks k, master.languages l
WHERE k.code = 'GK01'
  AND l.code IN ('en', 'hi', 'te', 'ta', 'kn', 'mr')
ON CONFLICT (kiosk_id, language_id) DO NOTHING;

\echo '✓ 0801 seed xiphias tenant done.'
