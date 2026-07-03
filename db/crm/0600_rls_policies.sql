-- ============================================================================
-- 0600_rls_policies.sql
-- Enable + FORCE row-level security on every table, then attach per-operation
-- policies for gk_crm_app. Ported 1:1 from the Supabase chain with:
--   authenticated      -> gk_crm_app
--   auth.uid()         -> crm.current_user_id()
--   is_sales_manager() -> crm.is_sales_manager()
--   is_crm_user()      -> crm.is_crm_user()
--   service_role       -> (dropped: gk_crm_backend has BYPASSRLS)
--
-- Per-op policies only (never FOR ALL) so the matrix stays reviewable and a
-- restrictive deny can never swallow a SELECT (the bug the Supabase
-- 0400_fixes migration had to undo).
--
-- gk_crm_backend bypasses RLS entirely. gk_crm_readonly gets a permissive
-- SELECT on every table (internal BI; the CRM is single-org, not multi-tenant).
--
-- Note on rep scoping: the EXISTS(... crm.leads ...) sub-checks run under the
-- app's own leads policy, so a rep only matches partners/tenants/contracts/
-- subscriptions whose source lead they still own and that is not soft-deleted.
-- ============================================================================

\echo '-- 0600 RLS policies --'

-- --- Enable + FORCE RLS on every table --------------------------------------
DO $$
DECLARE r record;
BEGIN
  FOR r IN
    SELECT n.nspname AS s, c.relname AS t
    FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE c.relkind = 'r' AND n.nspname IN ('crm','billing','audit')
    ORDER BY 1,2
  LOOP
    EXECUTE format('ALTER TABLE %I.%I ENABLE ROW LEVEL SECURITY', r.s, r.t);
    EXECUTE format('ALTER TABLE %I.%I FORCE  ROW LEVEL SECURITY', r.s, r.t);
  END LOOP;
END $$;

-- ============================================================================
-- crm.profiles
-- ============================================================================
DROP POLICY IF EXISTS profiles_app_select ON crm.profiles;
CREATE POLICY profiles_app_select ON crm.profiles
  FOR SELECT TO gk_crm_app
  USING ( id = crm.current_user_id() OR crm.is_sales_manager() );

DROP POLICY IF EXISTS profiles_app_update ON crm.profiles;
CREATE POLICY profiles_app_update ON crm.profiles
  FOR UPDATE TO gk_crm_app
  USING ( id = crm.current_user_id() )
  WITH CHECK ( id = crm.current_user_id() AND role = crm.current_user_role() );
-- INSERT/DELETE: gk_crm_backend only (BYPASSRLS) - no app policy.

-- ============================================================================
-- crm.leads
-- ============================================================================
DROP POLICY IF EXISTS leads_app_select ON crm.leads;
CREATE POLICY leads_app_select ON crm.leads
  FOR SELECT TO gk_crm_app
  USING ( deleted_at IS NULL AND ( crm.is_sales_manager() OR owner_id = crm.current_user_id() ) );

DROP POLICY IF EXISTS leads_app_insert ON crm.leads;
CREATE POLICY leads_app_insert ON crm.leads
  FOR INSERT TO gk_crm_app
  WITH CHECK ( crm.is_crm_user() AND ( owner_id = crm.current_user_id() OR crm.is_sales_manager() ) );

-- USING adds `deleted_at IS NULL` so the app role can only target LIVE leads —
-- a soft-deleted lead can no longer be edited or un-deleted (resurrected) via a
-- direct UPDATE, keeping the mutation policy consistent with leads_app_select.
-- (FORCE RLS also independently blocks moving a row out of SELECT visibility, so
-- a plain app UPDATE can never *set* deleted_at either; this clause makes the
-- "hands off soft-deleted rows" rule explicit rather than implicit.) Soft-delete
-- and permanent purge are privileged operations performed by gk_crm_backend
-- (BYPASSRLS) — a future SECURITY DEFINER archive RPC, mirroring move_lead_stage.
DROP POLICY IF EXISTS leads_app_update ON crm.leads;
CREATE POLICY leads_app_update ON crm.leads
  FOR UPDATE TO gk_crm_app
  USING ( deleted_at IS NULL AND ( crm.is_sales_manager() OR owner_id = crm.current_user_id() ) )
  WITH CHECK ( crm.is_sales_manager() OR owner_id = crm.current_user_id() );

-- Hard DELETE is manager-only AND limited to live rows; permanently purging an
-- already soft-deleted lead is a gk_crm_backend (BYPASSRLS) operation, not an
-- app-role one.
DROP POLICY IF EXISTS leads_app_delete ON crm.leads;
CREATE POLICY leads_app_delete ON crm.leads
  FOR DELETE TO gk_crm_app
  USING ( crm.is_sales_manager() AND deleted_at IS NULL );

-- ============================================================================
-- crm.lead_activities
-- ============================================================================
DROP POLICY IF EXISTS lead_activities_app_select ON crm.lead_activities;
CREATE POLICY lead_activities_app_select ON crm.lead_activities
  FOR SELECT TO gk_crm_app
  USING (
    crm.is_sales_manager()
    OR EXISTS ( SELECT 1 FROM crm.leads l
                 WHERE l.id = lead_activities.lead_id AND l.owner_id = crm.current_user_id() )
  );

DROP POLICY IF EXISTS lead_activities_app_insert ON crm.lead_activities;
CREATE POLICY lead_activities_app_insert ON crm.lead_activities
  FOR INSERT TO gk_crm_app
  WITH CHECK (
    crm.is_sales_manager()
    OR EXISTS ( SELECT 1 FROM crm.leads l
                 WHERE l.id = lead_activities.lead_id AND l.owner_id = crm.current_user_id() )
  );
-- UPDATE/DELETE: gk_crm_backend only.

-- ============================================================================
-- crm.partners
-- ============================================================================
DROP POLICY IF EXISTS partners_app_select ON crm.partners;
CREATE POLICY partners_app_select ON crm.partners
  FOR SELECT TO gk_crm_app
  USING (
    crm.is_sales_manager()
    OR EXISTS ( SELECT 1 FROM crm.leads l
                 WHERE l.id = partners.lead_id AND l.owner_id = crm.current_user_id() )
  );

-- Partners are created ONLY via crm.mark_lead_won_and_create_partner (SECURITY
-- DEFINER, runs as gk_crm_backend, re-checks lead ownership AND writes the
-- audit.audit_log entry). A direct app INSERT would bypass that audited path and
-- let a rep plant a partner with lead_id = NULL, so deny it outright (same
-- pattern as activation_keys). The RPC bypasses RLS, so this does not block it.
DROP POLICY IF EXISTS partners_app_insert ON crm.partners;
DROP POLICY IF EXISTS partners_no_insert  ON crm.partners;
CREATE POLICY partners_no_insert ON crm.partners
  AS RESTRICTIVE FOR INSERT TO gk_crm_app WITH CHECK ( false );

DROP POLICY IF EXISTS partners_app_update ON crm.partners;
CREATE POLICY partners_app_update ON crm.partners
  FOR UPDATE TO gk_crm_app
  USING (
    crm.is_sales_manager()
    OR EXISTS ( SELECT 1 FROM crm.leads l
                 WHERE l.id = partners.lead_id AND l.owner_id = crm.current_user_id() )
  )
  WITH CHECK ( crm.is_crm_user() );

DROP POLICY IF EXISTS partners_app_delete ON crm.partners;
CREATE POLICY partners_app_delete ON crm.partners
  FOR DELETE TO gk_crm_app
  USING ( crm.is_sales_manager() );

-- ============================================================================
-- crm.tenants
-- ============================================================================
DROP POLICY IF EXISTS tenants_app_select ON crm.tenants;
CREATE POLICY tenants_app_select ON crm.tenants
  FOR SELECT TO gk_crm_app
  USING (
    crm.is_sales_manager()
    OR EXISTS ( SELECT 1 FROM crm.partners p JOIN crm.leads l ON l.id = p.lead_id
                 WHERE p.id = tenants.partner_id AND l.owner_id = crm.current_user_id() )
  );

DROP POLICY IF EXISTS tenants_app_insert ON crm.tenants;
CREATE POLICY tenants_app_insert ON crm.tenants
  FOR INSERT TO gk_crm_app WITH CHECK ( crm.is_sales_manager() );

DROP POLICY IF EXISTS tenants_app_update ON crm.tenants;
CREATE POLICY tenants_app_update ON crm.tenants
  FOR UPDATE TO gk_crm_app USING ( crm.is_sales_manager() ) WITH CHECK ( crm.is_sales_manager() );

DROP POLICY IF EXISTS tenants_app_delete ON crm.tenants;
CREATE POLICY tenants_app_delete ON crm.tenants
  FOR DELETE TO gk_crm_app USING ( crm.is_sales_manager() );

-- ============================================================================
-- crm.activation_keys
-- SELECT for managers (forensic). Reps use crm.list_activation_keys(). All
-- writes go through the SECURITY DEFINER licensing RPCs - direct app writes
-- are explicitly denied per-op.
-- ============================================================================
DROP POLICY IF EXISTS activation_keys_app_select ON crm.activation_keys;
CREATE POLICY activation_keys_app_select ON crm.activation_keys
  FOR SELECT TO gk_crm_app USING ( crm.is_sales_manager() );

DROP POLICY IF EXISTS activation_keys_no_insert ON crm.activation_keys;
CREATE POLICY activation_keys_no_insert ON crm.activation_keys
  AS RESTRICTIVE FOR INSERT TO gk_crm_app WITH CHECK ( false );
DROP POLICY IF EXISTS activation_keys_no_update ON crm.activation_keys;
CREATE POLICY activation_keys_no_update ON crm.activation_keys
  AS RESTRICTIVE FOR UPDATE TO gk_crm_app USING ( false ) WITH CHECK ( false );
DROP POLICY IF EXISTS activation_keys_no_delete ON crm.activation_keys;
CREATE POLICY activation_keys_no_delete ON crm.activation_keys
  AS RESTRICTIVE FOR DELETE TO gk_crm_app USING ( false );

-- ============================================================================
-- crm.contracts
-- ============================================================================
DROP POLICY IF EXISTS contracts_app_select ON crm.contracts;
CREATE POLICY contracts_app_select ON crm.contracts
  FOR SELECT TO gk_crm_app
  USING (
    crm.is_sales_manager()
    OR EXISTS ( SELECT 1 FROM crm.partners p JOIN crm.leads l ON l.id = p.lead_id
                 WHERE p.id = contracts.partner_id AND l.owner_id = crm.current_user_id() )
  );

-- Scope the insert to a partner the caller can actually see (same predicate as
-- contracts_app_select / _update). The old WITH CHECK ( is_crm_user() ) let any
-- rep plant a contract — document_url, financial terms — against ANY partner_id,
-- including partners they can't read, polluting the manager-visible contract list.
DROP POLICY IF EXISTS contracts_app_insert ON crm.contracts;
CREATE POLICY contracts_app_insert ON crm.contracts
  FOR INSERT TO gk_crm_app
  WITH CHECK (
    crm.is_sales_manager()
    OR EXISTS ( SELECT 1 FROM crm.partners p JOIN crm.leads l ON l.id = p.lead_id
                 WHERE p.id = contracts.partner_id AND l.owner_id = crm.current_user_id() )
  );

DROP POLICY IF EXISTS contracts_app_update ON crm.contracts;
CREATE POLICY contracts_app_update ON crm.contracts
  FOR UPDATE TO gk_crm_app USING ( crm.is_sales_manager() ) WITH CHECK ( crm.is_sales_manager() );

DROP POLICY IF EXISTS contracts_app_delete ON crm.contracts;
CREATE POLICY contracts_app_delete ON crm.contracts
  FOR DELETE TO gk_crm_app USING ( crm.is_sales_manager() );

-- ============================================================================
-- crm.user_preferences (a user sees/edits only their own row)
-- ============================================================================
DROP POLICY IF EXISTS user_prefs_self_select ON crm.user_preferences;
CREATE POLICY user_prefs_self_select ON crm.user_preferences
  FOR SELECT TO gk_crm_app USING ( user_id = crm.current_user_id() );
DROP POLICY IF EXISTS user_prefs_self_insert ON crm.user_preferences;
CREATE POLICY user_prefs_self_insert ON crm.user_preferences
  FOR INSERT TO gk_crm_app WITH CHECK ( user_id = crm.current_user_id() );
DROP POLICY IF EXISTS user_prefs_self_update ON crm.user_preferences;
CREATE POLICY user_prefs_self_update ON crm.user_preferences
  FOR UPDATE TO gk_crm_app USING ( user_id = crm.current_user_id() ) WITH CHECK ( user_id = crm.current_user_id() );

-- ============================================================================
-- audit.audit_log (manager SELECT only; writes via DEFINER RPCs only)
-- ============================================================================
DROP POLICY IF EXISTS audit_log_app_select ON audit.audit_log;
CREATE POLICY audit_log_app_select ON audit.audit_log
  FOR SELECT TO gk_crm_app USING ( crm.is_sales_manager() );

DROP POLICY IF EXISTS audit_log_no_insert ON audit.audit_log;
CREATE POLICY audit_log_no_insert ON audit.audit_log
  AS RESTRICTIVE FOR INSERT TO gk_crm_app WITH CHECK ( false );
DROP POLICY IF EXISTS audit_log_no_update ON audit.audit_log;
CREATE POLICY audit_log_no_update ON audit.audit_log
  AS RESTRICTIVE FOR UPDATE TO gk_crm_app USING ( false ) WITH CHECK ( false );
DROP POLICY IF EXISTS audit_log_no_delete ON audit.audit_log;
CREATE POLICY audit_log_no_delete ON audit.audit_log
  AS RESTRICTIVE FOR DELETE TO gk_crm_app USING ( false );

-- ============================================================================
-- billing.subscriptions (same scope shape as partners; writes manager-only)
-- ============================================================================
DROP POLICY IF EXISTS subscriptions_app_select ON billing.subscriptions;
CREATE POLICY subscriptions_app_select ON billing.subscriptions
  FOR SELECT TO gk_crm_app
  USING (
    crm.is_sales_manager()
    OR EXISTS ( SELECT 1 FROM crm.partners p JOIN crm.leads l ON l.id = p.lead_id
                 WHERE p.id = subscriptions.partner_id AND l.owner_id = crm.current_user_id() )
  );

DROP POLICY IF EXISTS subscriptions_app_insert ON billing.subscriptions;
CREATE POLICY subscriptions_app_insert ON billing.subscriptions
  FOR INSERT TO gk_crm_app WITH CHECK ( crm.is_sales_manager() );
DROP POLICY IF EXISTS subscriptions_app_update ON billing.subscriptions;
CREATE POLICY subscriptions_app_update ON billing.subscriptions
  FOR UPDATE TO gk_crm_app USING ( crm.is_sales_manager() ) WITH CHECK ( crm.is_sales_manager() );
DROP POLICY IF EXISTS subscriptions_app_delete ON billing.subscriptions;
CREATE POLICY subscriptions_app_delete ON billing.subscriptions
  FOR DELETE TO gk_crm_app USING ( crm.is_sales_manager() );

-- ============================================================================
-- billing.subscription_periods (SELECT scoped; writes via DEFINER RPCs only)
-- ============================================================================
DROP POLICY IF EXISTS subscription_periods_app_select ON billing.subscription_periods;
CREATE POLICY subscription_periods_app_select ON billing.subscription_periods
  FOR SELECT TO gk_crm_app
  USING (
    crm.is_sales_manager()
    OR EXISTS ( SELECT 1 FROM billing.subscriptions s
                 JOIN crm.partners p ON p.id = s.partner_id
                 JOIN crm.leads    l ON l.id = p.lead_id
                 WHERE s.id = subscription_periods.subscription_id AND l.owner_id = crm.current_user_id() )
  );

DROP POLICY IF EXISTS subscription_periods_no_insert ON billing.subscription_periods;
CREATE POLICY subscription_periods_no_insert ON billing.subscription_periods
  AS RESTRICTIVE FOR INSERT TO gk_crm_app WITH CHECK ( false );
DROP POLICY IF EXISTS subscription_periods_no_update ON billing.subscription_periods;
CREATE POLICY subscription_periods_no_update ON billing.subscription_periods
  AS RESTRICTIVE FOR UPDATE TO gk_crm_app USING ( false ) WITH CHECK ( false );
DROP POLICY IF EXISTS subscription_periods_no_delete ON billing.subscription_periods;
CREATE POLICY subscription_periods_no_delete ON billing.subscription_periods
  AS RESTRICTIVE FOR DELETE TO gk_crm_app USING ( false );

-- ============================================================================
-- billing.renewal_reminders (SELECT scoped; writes via DEFINER RPCs only)
-- ============================================================================
DROP POLICY IF EXISTS renewal_reminders_app_select ON billing.renewal_reminders;
CREATE POLICY renewal_reminders_app_select ON billing.renewal_reminders
  FOR SELECT TO gk_crm_app
  USING (
    crm.is_sales_manager()
    OR EXISTS ( SELECT 1 FROM billing.subscriptions s
                 JOIN crm.partners p ON p.id = s.partner_id
                 JOIN crm.leads    l ON l.id = p.lead_id
                 WHERE s.id = renewal_reminders.subscription_id AND l.owner_id = crm.current_user_id() )
  );

DROP POLICY IF EXISTS renewal_reminders_no_insert ON billing.renewal_reminders;
CREATE POLICY renewal_reminders_no_insert ON billing.renewal_reminders
  AS RESTRICTIVE FOR INSERT TO gk_crm_app WITH CHECK ( false );
DROP POLICY IF EXISTS renewal_reminders_no_update ON billing.renewal_reminders;
CREATE POLICY renewal_reminders_no_update ON billing.renewal_reminders
  AS RESTRICTIVE FOR UPDATE TO gk_crm_app USING ( false ) WITH CHECK ( false );
DROP POLICY IF EXISTS renewal_reminders_no_delete ON billing.renewal_reminders;
CREATE POLICY renewal_reminders_no_delete ON billing.renewal_reminders
  AS RESTRICTIVE FOR DELETE TO gk_crm_app USING ( false );

-- ============================================================================
-- billing.plan_catalog (any CRM user reads; manager writes)
-- ============================================================================
DROP POLICY IF EXISTS plan_catalog_app_select ON billing.plan_catalog;
CREATE POLICY plan_catalog_app_select ON billing.plan_catalog
  FOR SELECT TO gk_crm_app USING ( crm.is_crm_user() );
DROP POLICY IF EXISTS plan_catalog_app_insert ON billing.plan_catalog;
CREATE POLICY plan_catalog_app_insert ON billing.plan_catalog
  FOR INSERT TO gk_crm_app WITH CHECK ( crm.is_sales_manager() );
DROP POLICY IF EXISTS plan_catalog_app_update ON billing.plan_catalog;
CREATE POLICY plan_catalog_app_update ON billing.plan_catalog
  FOR UPDATE TO gk_crm_app USING ( crm.is_sales_manager() ) WITH CHECK ( crm.is_sales_manager() );
DROP POLICY IF EXISTS plan_catalog_app_delete ON billing.plan_catalog;
CREATE POLICY plan_catalog_app_delete ON billing.plan_catalog
  FOR DELETE TO gk_crm_app USING ( crm.is_sales_manager() );

-- ============================================================================
-- billing.stripe_event_log + ses_message_log (manager SELECT; writes backend)
-- ============================================================================
DROP POLICY IF EXISTS stripe_event_log_app_select ON billing.stripe_event_log;
CREATE POLICY stripe_event_log_app_select ON billing.stripe_event_log
  FOR SELECT TO gk_crm_app USING ( crm.is_sales_manager() );
DROP POLICY IF EXISTS stripe_event_log_no_insert ON billing.stripe_event_log;
CREATE POLICY stripe_event_log_no_insert ON billing.stripe_event_log
  AS RESTRICTIVE FOR INSERT TO gk_crm_app WITH CHECK ( false );
DROP POLICY IF EXISTS stripe_event_log_no_update ON billing.stripe_event_log;
CREATE POLICY stripe_event_log_no_update ON billing.stripe_event_log
  AS RESTRICTIVE FOR UPDATE TO gk_crm_app USING ( false ) WITH CHECK ( false );
DROP POLICY IF EXISTS stripe_event_log_no_delete ON billing.stripe_event_log;
CREATE POLICY stripe_event_log_no_delete ON billing.stripe_event_log
  AS RESTRICTIVE FOR DELETE TO gk_crm_app USING ( false );

DROP POLICY IF EXISTS ses_message_log_app_select ON billing.ses_message_log;
CREATE POLICY ses_message_log_app_select ON billing.ses_message_log
  FOR SELECT TO gk_crm_app USING ( crm.is_sales_manager() );
DROP POLICY IF EXISTS ses_message_log_no_insert ON billing.ses_message_log;
CREATE POLICY ses_message_log_no_insert ON billing.ses_message_log
  AS RESTRICTIVE FOR INSERT TO gk_crm_app WITH CHECK ( false );
DROP POLICY IF EXISTS ses_message_log_no_update ON billing.ses_message_log;
CREATE POLICY ses_message_log_no_update ON billing.ses_message_log
  AS RESTRICTIVE FOR UPDATE TO gk_crm_app USING ( false ) WITH CHECK ( false );
DROP POLICY IF EXISTS ses_message_log_no_delete ON billing.ses_message_log;
CREATE POLICY ses_message_log_no_delete ON billing.ses_message_log
  AS RESTRICTIVE FOR DELETE TO gk_crm_app USING ( false );

-- ============================================================================
-- gk_crm_readonly: permissive SELECT on every table (internal BI)
-- ============================================================================
DO $$
DECLARE r record;
BEGIN
  FOR r IN
    SELECT n.nspname AS s, c.relname AS t
    FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
    WHERE c.relkind = 'r' AND n.nspname IN ('crm','billing','audit')
    ORDER BY 1,2
  LOOP
    EXECUTE format('DROP POLICY IF EXISTS %I ON %I.%I', r.t || '_ro_select', r.s, r.t);
    EXECUTE format('CREATE POLICY %I ON %I.%I FOR SELECT TO gk_crm_readonly USING (true)',
                   r.t || '_ro_select', r.s, r.t);
  END LOOP;
END $$;

-- --- verify -----------------------------------------------------------------
\echo '-- policies per table --'
SELECT schemaname, tablename, count(*) AS policies
FROM pg_policies
WHERE schemaname IN ('crm','billing','audit')
GROUP BY schemaname, tablename
ORDER BY schemaname, tablename;

\echo 'ok: 0600 RLS policies done.'
