-- ============================================================================
-- 0803_tables_item_categories.sql
-- Schema: kiosk
-- Per-tenant item category configuration for the "What are you selling?" screen.
-- tenant_id = NULL  → default (shown to all tenants with no custom config)
-- tenant_id = <id>  → tenant-specific (takes priority over defaults)
-- Idempotent.
-- ============================================================================

\echo '── 0803 item_categories ──'

CREATE TABLE IF NOT EXISTS kiosk.item_categories (
    id             uuid    PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id      uuid    NULL,
    category_key   text    NOT NULL,
    display_name   text    NOT NULL,
    icon           text    NOT NULL DEFAULT '',
    ai_form_class  text    NOT NULL DEFAULT 'ANY',
    min_items      int     NOT NULL DEFAULT 1,
    max_items      int     NOT NULL DEFAULT 1,
    display_order  int     NOT NULL DEFAULT 0,
    is_active      boolean NOT NULL DEFAULT true,
    created_at     timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT fk_item_categories__tenants
        FOREIGN KEY (tenant_id) REFERENCES tenancy.tenants(id) ON DELETE CASCADE,
    CONSTRAINT uq_item_categories__tenant_key
        UNIQUE (tenant_id, category_key)
);

CREATE INDEX IF NOT EXISTS ix_item_categories__tenant
    ON kiosk.item_categories(tenant_id, display_order) WHERE is_active = true;

CREATE INDEX IF NOT EXISTS ix_item_categories__defaults
    ON kiosk.item_categories(display_order) WHERE tenant_id IS NULL AND is_active = true;

-- ─── Default categories (tenant_id = NULL — shown to all tenants with no custom config) ───
INSERT INTO kiosk.item_categories (tenant_id, category_key, display_name, icon, ai_form_class, min_items, max_items, display_order)
VALUES
    (NULL, 'Ring',                  'Ring',                        '💍', 'RING',                 1, 1, 1),
    (NULL, 'ChainNecklaceBracelet', 'Chain / Necklace / Bracelet', '📿', 'STRAND|RIGID_BAND',    1, 1, 2),
    (NULL, 'PendantCharm',          'Pendant / Charm',             '✨', 'PENDANT_CHARM|STRAND', 1, 1, 3),
    (NULL, 'EarringsCufflinks',     'Earrings / Cufflinks',        '💎', 'EARRINGS',             1, 2, 4),
    (NULL, 'Watch',                 'Watch',                       '⌚', 'WATCH',                1, 1, 5),
    (NULL, 'CoinBar',               'Coin / Bar',                  '🪙', 'BULLION',              1, 1, 6),
    (NULL, 'Other',                 'Other',                       '📦', 'ANY',                  1, 1, 7)
ON CONFLICT (tenant_id, category_key) DO NOTHING;

-- ─── XIPHIAS tenant-specific categories (same set — tenant can customise later) ────────────
INSERT INTO kiosk.item_categories (tenant_id, category_key, display_name, icon, ai_form_class, min_items, max_items, display_order)
SELECT t.id, v.category_key, v.display_name, v.icon, v.ai_form_class, v.min_items, v.max_items, v.display_order
FROM tenancy.tenants t,
(VALUES
    ('Ring',                  'Ring',                        '💍', 'RING',                 1, 1, 1),
    ('ChainNecklaceBracelet', 'Chain / Necklace / Bracelet', '📿', 'STRAND|RIGID_BAND',    1, 1, 2),
    ('PendantCharm',          'Pendant / Charm',             '✨', 'PENDANT_CHARM|STRAND', 1, 1, 3),
    ('EarringsCufflinks',     'Earrings / Cufflinks',        '💎', 'EARRINGS',             1, 2, 4),
    ('Watch',                 'Watch',                       '⌚', 'WATCH',                1, 1, 5),
    ('CoinBar',               'Coin / Bar',                  '🪙', 'BULLION',              1, 1, 6),
    ('Other',                 'Other',                       '📦', 'ANY',                  1, 1, 7)
) AS v(category_key, display_name, icon, ai_form_class, min_items, max_items, display_order)
WHERE t.code = 'XIPHIAS'
ON CONFLICT (tenant_id, category_key) DO NOTHING;

\echo '✓ 0803 item_categories done.'
