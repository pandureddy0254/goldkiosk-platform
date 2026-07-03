\echo '── 0035 master schema + languages + kiosk_languages ──'

-- ─── master schema ───────────────────────────────────────────────────────────
CREATE SCHEMA IF NOT EXISTS master;

-- ─── master.languages ────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS master.languages (
    id              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    code            text        NOT NULL,
    english_name    text        NOT NULL,
    native_name     text        NOT NULL,
    default_order   int         NOT NULL DEFAULT 0,
    is_active       boolean     NOT NULL DEFAULT true,
    CONSTRAINT uq_languages__code UNIQUE (code)
);

INSERT INTO master.languages (code, english_name, native_name, default_order) VALUES
    ('en', 'English', 'English',  1),
    ('hi', 'Hindi',   'हिंदी',    2),
    ('te', 'Telugu',  'తెలుగు',  3),
    ('ta', 'Tamil',   'தமிழ்',   4),
    ('kn', 'Kannada', 'ಕನ್ನಡ',  5),
    ('mr', 'Marathi', 'मराठी',   6),
    ('bn', 'Bengali', 'বাংলা',   7)
ON CONFLICT (code) DO NOTHING;

-- ─── kiosk.kiosk_languages ───────────────────────────────────────────────────
-- Per-kiosk language ordering. If a kiosk has no rows here the API falls back
-- to all active master.languages ordered by default_order.
CREATE TABLE IF NOT EXISTS kiosk.kiosk_languages (
    id              uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    kiosk_id        uuid        NOT NULL REFERENCES kiosk.kiosks(id) ON DELETE CASCADE,
    language_id     uuid        NOT NULL REFERENCES master.languages(id) ON DELETE CASCADE,
    display_order   int         NOT NULL DEFAULT 0,
    is_active       boolean     NOT NULL DEFAULT true,
    CONSTRAINT uq_kiosk_languages__kiosk_lang UNIQUE (kiosk_id, language_id)
);

CREATE INDEX IF NOT EXISTS ix_kiosk_languages__kiosk ON kiosk.kiosk_languages(kiosk_id);

\echo '✓ 0035 done.'
