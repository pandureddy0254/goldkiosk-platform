-- ============================================================================
-- 0005_add_superadmin_role.sql
-- No-op (kept as a numbered file for ordering).
--
-- Superadmin is the bootstrap CRM administrator: full CRM access (treated as
-- >= sales_manager everywhere — see crm.is_sales_manager in 0100) PLUS the only
-- role permitted to create new CRM members (see crm.create_member in 0304).
--
-- Previously this added 'superadmin' to the crm.crm_role enum. The CRM no
-- longer uses native enum types: 'superadmin' is now simply one of the allowed
-- values in the crm.profiles.role CHECK constraint
-- (ck_profiles__role IN ('sales_manager','sales_rep','superadmin'), defined in
-- 0010), so no ALTER TYPE is needed.
-- ============================================================================

\echo '-- 0005 superadmin role (allowed in crm.profiles.role CHECK; no-op) --'

\echo 'ok: 0005 superadmin role present.'
