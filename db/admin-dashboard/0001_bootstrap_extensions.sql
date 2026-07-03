-- ============================================================================
-- 0001_bootstrap_extensions.sql
-- Extensions, custom domains, generic helper functions.
-- Idempotent. Safe to re-run.
-- Target: goldkiosk_<env> (any env)
-- ============================================================================

\echo '────────────────────────────────────────────────────────────────────────'
\echo '0001 — Bootstrap: extensions, domains, helpers'
\echo '────────────────────────────────────────────────────────────────────────'

-- ─── Extensions ──────────────────────────────────────────────────────────────
-- pgcrypto:           gen_random_uuid() + pgp_sym_encrypt/decrypt for PII
-- citext:             case-insensitive text for emails, codes
-- pg_trgm:            trigram fuzzy search for admin lookups
-- btree_gin:          composite GIN indexes over (tenant_id, …) etc.
-- pg_stat_statements: query telemetry; surfaces in Azure Query Performance Insight

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS btree_gin;
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;

\echo '  ✓ extensions installed'

-- ─── Custom domains (single source of truth for common shapes) ───────────────
-- Domains live in `public` so every schema can use them without qualification.

DO $$ BEGIN
  CREATE DOMAIN public.domain_money AS numeric(18,4)
    CHECK (VALUE > -1e15 AND VALUE < 1e15);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
  CREATE DOMAIN public.domain_grams AS numeric(12,4)
    CHECK (VALUE >= 0);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
  CREATE DOMAIN public.domain_karat AS smallint
    CHECK (VALUE IN (8, 9, 10, 14, 18, 22, 24));
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
  CREATE DOMAIN public.domain_percent AS numeric(8,4)
    CHECK (VALUE >= 0 AND VALUE <= 100);
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
  CREATE DOMAIN public.domain_currency_code AS char(3)
    CHECK (VALUE ~ '^[A-Z]{3}$');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
  CREATE DOMAIN public.domain_country_code AS char(2)
    CHECK (VALUE ~ '^[A-Z]{2}$');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
  CREATE DOMAIN public.domain_locale_tag AS text
    CHECK (VALUE ~ '^[a-z]{2,3}(-[A-Z][a-z]{3})?(-[A-Z]{2})?$');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

DO $$ BEGIN
  CREATE DOMAIN public.domain_email AS citext
    CHECK (VALUE ~* '^[^@[:space:]]+@[^@[:space:]]+\.[^@[:space:]]+$');
EXCEPTION WHEN duplicate_object THEN NULL; END $$;

\echo '  ✓ domains created'

-- ─── Generic helper: maintain updated_at on UPDATE ───────────────────────────
-- Attached to every table that has an `updated_at` column via a per-table
-- trigger created in the 002x_triggers files.

CREATE OR REPLACE FUNCTION public.fn_set_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$;

COMMENT ON FUNCTION public.fn_set_updated_at()
  IS 'Generic BEFORE UPDATE trigger function: sets NEW.updated_at = now(). Attach via CREATE TRIGGER tg_<table>__bu_set_updated_at.';

\echo '  ✓ fn_set_updated_at() created'

-- ─── Generic helper: reject UPDATE/DELETE on append-only tables ──────────────

CREATE OR REPLACE FUNCTION public.fn_reject_modify()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Table %.% is append-only; %s is not permitted',
        TG_TABLE_SCHEMA, TG_TABLE_NAME, TG_OP;
END;
$$;

COMMENT ON FUNCTION public.fn_reject_modify()
  IS 'Generic BEFORE UPDATE OR DELETE trigger function for append-only tables. Attach via CREATE TRIGGER tg_<table>__bud_immutable.';

\echo '  ✓ fn_reject_modify() created'

-- ─── Verification ────────────────────────────────────────────────────────────
\echo ''
\echo '── Extensions present ──'
SELECT extname, extversion FROM pg_extension WHERE extname <> 'plpgsql' ORDER BY extname;

\echo ''
\echo '── Domains present ──'
SELECT n.nspname AS schema, t.typname AS domain
FROM pg_type t JOIN pg_namespace n ON n.oid = t.typnamespace
WHERE t.typtype = 'd' AND n.nspname = 'public'
ORDER BY t.typname;

\echo ''
\echo '── Helper functions present ──'
SELECT n.nspname AS schema, p.proname AS function
FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
WHERE n.nspname = 'public' AND p.proname IN ('fn_set_updated_at', 'fn_reject_modify')
ORDER BY p.proname;

\echo ''
\echo '✓ 0001 complete.'
