-- ============================================================================
-- 0804_alter_screensavers_global_defaults.sql
-- Schema: kiosk
-- Makes tenant_id nullable on screen_savers so global default slides can exist.
-- tenant_id = NULL  → global default (shown to any tenant with no custom slides)
-- tenant_id = <id>  → tenant-specific (takes priority over global defaults)
-- Idempotent.
-- ============================================================================

\echo '── 0804 screensavers global defaults ──'

-- Allow NULL tenant_id (was NOT NULL)
ALTER TABLE kiosk.screen_savers
    ALTER COLUMN tenant_id DROP NOT NULL;

-- Drop the old unique index (tenant_id NOT NULL assumed); recreate allowing NULLs.
-- PostgreSQL unique indexes treat each NULL as distinct, so multiple NULL-tenant rows
-- are allowed — uniqueness is only enforced within the same tenant.
ALTER TABLE kiosk.screen_savers DROP CONSTRAINT IF EXISTS uq_screen_savers__tenant_code;
DROP INDEX IF EXISTS kiosk.uq_screen_savers__tenant_code;

CREATE UNIQUE INDEX IF NOT EXISTS uq_screen_savers__tenant_code
    ON kiosk.screen_savers (tenant_id, code)
    WHERE tenant_id IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS uq_screen_savers__global_code
    ON kiosk.screen_savers (code)
    WHERE tenant_id IS NULL;

-- Seed 3 global default slides (idempotent via ON CONFLICT DO NOTHING)
INSERT INTO kiosk.screen_savers (id, tenant_id, code, image_url, media_type, duration_seconds, display_order, is_active, created_at)
VALUES
    (gen_random_uuid(), NULL, 'GK-DEFAULT-SS-01',
     'https://placehold.co/1920x1080/D7AC00/FFFFFF?text=GoldKiosk+%7C+Instant+Gold+Buying',
     'image', 8, 1, true, now()),

    (gen_random_uuid(), NULL, 'GK-DEFAULT-SS-02',
     'https://placehold.co/1920x1080/75521F/FFFFFF?text=Instant+Cash+for+Your+Gold+%26+Silver',
     'image', 8, 2, true, now()),

    (gen_random_uuid(), NULL, 'GK-DEFAULT-SS-03',
     'https://placehold.co/1920x1080/B8862F/FFFFFF?text=Safe+%7C+Instant+%7C+Trusted',
     'image', 8, 3, true, now())
ON CONFLICT DO NOTHING;

\echo '── 0804 done ──'
