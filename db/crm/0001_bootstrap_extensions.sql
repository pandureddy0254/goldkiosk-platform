-- ============================================================================
-- 0001_bootstrap_extensions.sql
-- Extensions + generic helper functions (public).
-- Idempotent. Safe to re-run.
-- Target: goldkiosk_crm_<env> (any env)
-- ============================================================================

\echo '------------------------------------------------------------------------'
\echo '0001 - Bootstrap: extensions + helper functions'
\echo '------------------------------------------------------------------------'

-- --- Extensions -------------------------------------------------------------
-- pgcrypto:  gen_random_uuid(), gen_random_bytes(), digest() (SHA-256 of keys),
--            crypt()/gen_salt() for demo password hashes.
-- citext:    case-insensitive text for emails.
-- pg_trgm:   trigram fuzzy search for the partners/leads admin lookups.
-- On plain PostgreSQL these install into `public` (unlike Supabase, which puts
-- pgcrypto in an `extensions` schema). That is why this port never needs the
-- `set search_path = public, extensions` workaround the Supabase chain carried.

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

\echo '  ok: extensions installed'

-- --- Generic helper: maintain updated_at on UPDATE --------------------------
-- Attached to every table that has an `updated_at` column by 0200.

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
  IS 'Generic BEFORE UPDATE trigger: sets NEW.updated_at = now(). Attached by 0200.';

\echo '  ok: fn_set_updated_at() created'

-- --- Generic helper: reject UPDATE/DELETE on append-only tables -------------

CREATE OR REPLACE FUNCTION public.fn_reject_modify()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Table %.% is append-only; % is not permitted',
        TG_TABLE_SCHEMA, TG_TABLE_NAME, TG_OP
        USING ERRCODE = '42501';
END;
$$;

COMMENT ON FUNCTION public.fn_reject_modify()
  IS 'Generic BEFORE UPDATE OR DELETE trigger for append-only tables. Attached by 0201.';

\echo '  ok: fn_reject_modify() created'

-- --- Verification -----------------------------------------------------------
\echo ''
\echo '-- Extensions present --'
SELECT extname, extversion FROM pg_extension WHERE extname <> 'plpgsql' ORDER BY extname;

\echo ''
\echo 'ok: 0001 complete.'
