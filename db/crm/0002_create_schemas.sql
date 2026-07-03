-- ============================================================================
-- 0002_create_schemas.sql
-- Creates the three domain-grouped schemas + revokes PUBLIC defaults.
-- Idempotent. Safe to re-run.
-- Target: goldkiosk_crm_<env> (any env)
-- ============================================================================

\echo '------------------------------------------------------------------------'
\echo '0002 - Schemas: crm, billing, audit'
\echo '------------------------------------------------------------------------'

CREATE SCHEMA IF NOT EXISTS crm;       -- core CRM + the canonical enum namespace
CREATE SCHEMA IF NOT EXISTS billing;   -- subscriptions, invoices, reminders, Stripe/SES logs
CREATE SCHEMA IF NOT EXISTS audit;     -- append-only compliance trail

COMMENT ON SCHEMA crm     IS 'Core CRM: staff profiles, leads, partners, tenants, contracts, activation keys, prefs. Also holds every CRM enum type.';
COMMENT ON SCHEMA billing IS 'Partner subscriptions + billing history, renewal reminders, plan catalog, Stripe + SES integration logs.';
COMMENT ON SCHEMA audit   IS 'Append-only audit trail. Immutable by trigger; readable by sales_manager only (see RLS).';

\echo '  ok: 3 schemas created'

-- --- Revoke PUBLIC defaults (lock down before granting per role in 0003) ----
REVOKE ALL ON SCHEMA crm, billing, audit FROM PUBLIC;

\echo '  ok: PUBLIC revoked from all schemas'

-- --- Verification -----------------------------------------------------------
\echo ''
\echo '-- Schemas (excluding system) --'
SELECT n.nspname AS schema,
       pg_catalog.obj_description(n.oid, 'pg_namespace') AS comment
FROM pg_namespace n
WHERE n.nspname IN ('crm','billing','audit')
ORDER BY n.nspname;

\echo ''
\echo 'ok: 0002 complete.'
