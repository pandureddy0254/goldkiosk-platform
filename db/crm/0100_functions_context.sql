-- ============================================================================
-- 0100_functions_context.sql
-- Per-request user context + RLS-helper predicates (schema crm).
--
-- These replace Supabase's auth.uid() / public.is_sales_manager() /
-- public.is_crm_user(). The app calls crm.set_user_context(<profile-id>) once
-- per request (right after BEGIN, as gk_crm_app); the policies in 0600 then
-- read it back.
--
-- The is_* / current_user_role helpers are SECURITY DEFINER and are re-owned
-- to gk_crm_backend in 0700, so they read crm.profiles bypassing RLS - this
-- is what prevents a profiles-policy <-> helper infinite recursion.
-- ============================================================================

\echo '-- 0100 context + RLS helpers --'

-- --- set the user GUC for RLS scoping (LOCAL = transaction-scoped) -----------
-- CONTRACT (read before changing the third arg of set_config):
--   * When calling this RPC directly (e.g. from a background job or psql
--     session), the third arg MUST stay `true` (LOCAL / transaction-scoped)
--     so the GUC is discarded at COMMIT/ROLLBACK and cannot leak across
--     pooled-connection reuse.
--   * The .NET web app does NOT call this RPC. Instead it uses an EF Core
--     DbConnectionInterceptor (UserContextInterceptor) that runs
--     set_config('app.user_id', $1, false) — session-scoped — on every
--     ConnectionOpened event. That is safe because Npgsql fires ConnectionOpened
--     on every pool checkout (every NpgsqlConnection.Open() call), so the GUC
--     is reset to the current request's user before any query executes. There
--     is no window for cross-request identity leakage with that pattern.
--   * If you add a non-interceptor call path (raw psql, pgAgent, background
--     worker), use true (LOCAL) here and call from inside a BEGIN/COMMIT block.
CREATE OR REPLACE FUNCTION crm.set_user_context(p_user_id uuid)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
BEGIN
    PERFORM set_config('app.user_id', COALESCE(p_user_id::text, ''), true);  -- true = LOCAL (transaction-scoped)
END;
$$;
COMMENT ON FUNCTION crm.set_user_context(uuid)
  IS 'Sets app.user_id for the current transaction (LOCAL). MUST be called inside the request transaction as gk_crm_app; RLS policies read it via crm.current_user_id(). Never session-scoped (pool-reuse identity leak).';

-- --- clear the user GUC (symmetry; same LOCAL/transaction rules apply) -------
CREATE OR REPLACE FUNCTION crm.reset_user_context()
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
BEGIN
    PERFORM set_config('app.user_id', '', true);  -- true = LOCAL (transaction-scoped)
END;
$$;
COMMENT ON FUNCTION crm.reset_user_context()
  IS 'Clears app.user_id for the current transaction (LOCAL). Optional companion to set_user_context.';

-- --- read the current user GUC ----------------------------------------------
CREATE OR REPLACE FUNCTION crm.current_user_id()
RETURNS uuid
LANGUAGE sql
STABLE
AS $$
    SELECT NULLIF(current_setting('app.user_id', true), '')::uuid;
$$;
COMMENT ON FUNCTION crm.current_user_id() IS 'The signed-in profile id from the app.user_id GUC (NULL if unset).';

-- --- the current user's role (bypasses RLS via DEFINER) ---------------------
CREATE OR REPLACE FUNCTION crm.current_user_role()
RETURNS text
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
    SELECT role FROM crm.profiles WHERE id = crm.current_user_id();
$$;

-- --- is the current user a sales_manager (or superadmin)? -------------------
-- superadmin is intentionally >= sales_manager: every manager-level RLS policy
-- and RPC check flows through this helper, so a superadmin gets full CRM access
-- without duplicating 'superadmin' into every policy.
CREATE OR REPLACE FUNCTION crm.is_sales_manager()
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
    SELECT EXISTS (
        SELECT 1 FROM crm.profiles
         WHERE id = crm.current_user_id() AND role IN ('sales_manager','superadmin')
    );
$$;

-- --- is the current user the superadmin? ------------------------------------
-- Gates member management (crm.create_member). Only the superadmin may create
-- new CRM logins; sales_manager/sales_rep cannot.
CREATE OR REPLACE FUNCTION crm.is_superadmin()
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
    SELECT EXISTS (
        SELECT 1 FROM crm.profiles
         WHERE id = crm.current_user_id() AND role = 'superadmin'
    );
$$;

-- --- is the current user any internal CRM user? -----------------------------
CREATE OR REPLACE FUNCTION crm.is_crm_user()
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = crm, public, pg_temp
AS $$
    SELECT EXISTS (SELECT 1 FROM crm.profiles WHERE id = crm.current_user_id());
$$;

-- --- grants (these run inside every RLS policy, so the app must EXECUTE) -----
REVOKE ALL ON FUNCTION crm.set_user_context(uuid)  FROM PUBLIC;
REVOKE ALL ON FUNCTION crm.reset_user_context()    FROM PUBLIC;
REVOKE ALL ON FUNCTION crm.current_user_id()       FROM PUBLIC;
REVOKE ALL ON FUNCTION crm.current_user_role()     FROM PUBLIC;
REVOKE ALL ON FUNCTION crm.is_sales_manager()      FROM PUBLIC;
REVOKE ALL ON FUNCTION crm.is_superadmin()         FROM PUBLIC;
REVOKE ALL ON FUNCTION crm.is_crm_user()           FROM PUBLIC;

GRANT EXECUTE ON FUNCTION crm.set_user_context(uuid)  TO gk_crm_app, gk_crm_backend;
GRANT EXECUTE ON FUNCTION crm.reset_user_context()    TO gk_crm_app, gk_crm_backend;
GRANT EXECUTE ON FUNCTION crm.current_user_id()       TO gk_crm_app, gk_crm_backend;
GRANT EXECUTE ON FUNCTION crm.current_user_role()     TO gk_crm_app, gk_crm_backend;
GRANT EXECUTE ON FUNCTION crm.is_sales_manager()      TO gk_crm_app, gk_crm_backend;
GRANT EXECUTE ON FUNCTION crm.is_superadmin()         TO gk_crm_app, gk_crm_backend;
GRANT EXECUTE ON FUNCTION crm.is_crm_user()           TO gk_crm_app, gk_crm_backend;

\echo 'ok: 0100 context helpers done.'
