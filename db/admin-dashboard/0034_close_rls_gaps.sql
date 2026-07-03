-- ============================================================================
-- 0034_close_rls_gaps.sql
-- Close residual RLS gaps on tenant-scoped tables that are read directly
-- (not via FK chain to a parent that's already RLS-protected).
--
-- Tables covered:
--   identity.activation_keys       — looked up by key_hash during /Account/Activate
--   identity.user_invitations      — looked up by token during /Account/AcceptInvite
--
-- Both already carry tenant_id but had no policy. With this migration, the
-- canonical predicate
--   tenant_id = (current_setting('app.tenant_id', true))::uuid
-- applies to every read/write, matching the 27 already-protected tables.
--
-- NOTE: audit.inbox_messages was originally flagged in the validation report
-- but inspection shows it has NO tenant_id column — it's a global idempotency
-- inbox keyed by (source, message_id). RLS does not apply. Dropped from scope.
--
-- PROD NOTE — pre-auth lookups
-- ----------------------------
-- /Account/Activate and /Account/AcceptInvite run BEFORE the user has signed
-- in, i.e. before TenantContextInterceptor has a tenant_id to set. With this
-- migration applied AND a non-superuser app role, those lookups would return
-- zero rows and the activation/invitation flows would break.
--
-- The intended production fix is the SECURITY DEFINER pair in migration 0033:
--
--   identity.validate_activation_key(p_key, p_email) RETURNS tenant_id, key_id, ...
--   identity.consume_activation_key(p_key_id, p_user_id)
--
-- These functions, when applied AND owned by a BYPASSRLS role (e.g. `gk_owner`),
-- read activation_keys with RLS off and hand the resolved tenant_id back to
-- the app. The same pattern must be applied to `user_invitations` (a
-- corresponding `identity.validate_user_invitation(p_token)` SECURITY DEFINER
-- function would be needed, currently missing).
--
-- LOCAL DEV NOTE
-- ---------------
-- The dev DB connects as the `postgres` superuser, which carries
-- `rolbypassrls = true` — these RLS policies will be bypassed automatically
-- on the local connection. Applying this migration in dev is safe: it makes
-- the schema production-ready without affecting current local behaviour.
--
-- Idempotent.
-- ============================================================================

\echo '── 0034 close residual RLS gaps ──'

BEGIN;

-- ── identity.activation_keys ────────────────────────────────────────────────
ALTER TABLE identity.activation_keys ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON identity.activation_keys;
CREATE POLICY tenant_isolation ON identity.activation_keys
    USING       (tenant_id = (current_setting('app.tenant_id', true))::uuid)
    WITH CHECK  (tenant_id = (current_setting('app.tenant_id', true))::uuid);

-- ── identity.user_invitations ───────────────────────────────────────────────
ALTER TABLE identity.user_invitations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON identity.user_invitations;
CREATE POLICY tenant_isolation ON identity.user_invitations
    USING       (tenant_id = (current_setting('app.tenant_id', true))::uuid)
    WITH CHECK  (tenant_id = (current_setting('app.tenant_id', true))::uuid);

COMMIT;

\echo '✓ 0034 done. 2 tenant-scoped tables now carry RLS.'
\echo '  Next step (prod): apply 0033 SECURITY DEFINER functions + author a similar'
\echo '  validate_user_invitation() function before deploying behind a non-superuser role.'
