-- ============================================================================
-- 0002_create_schemas.sql
-- Creates the 19 domain-grouped PG schemas + revokes PUBLIC defaults.
-- Idempotent. Safe to re-run.
-- Target: goldkiosk_<env> (any env)
-- ============================================================================

\echo '────────────────────────────────────────────────────────────────────────'
\echo '0002 — Schemas: 19 domain-grouped namespaces'
\echo '────────────────────────────────────────────────────────────────────────'

-- ─── Domain schemas ─────────────────────────────────────────────────────────
-- Order is alphabetical except: reporting is last because it consumes the rest.

CREATE SCHEMA IF NOT EXISTS audit;        -- audit_events, data_access_log, outbox/inbox messages
CREATE SCHEMA IF NOT EXISTS compliance;   -- retention policies, data subject requests, deletion jobs
CREATE SCHEMA IF NOT EXISTS config;       -- feature flags, app_configs, schedules
CREATE SCHEMA IF NOT EXISTS customer;     -- customers, profiles, identifiers, addresses, contacts, KYC, wallets, ledger
CREATE SCHEMA IF NOT EXISTS doc;          -- receipts, contracts, templates, signatures, statements
CREATE SCHEMA IF NOT EXISTS helpdesk;     -- feedback, ticket categories/sub, support tickets, SOS
CREATE SCHEMA IF NOT EXISTS identity;     -- users, MFA, sessions, roles, permissions, modules
CREATE SCHEMA IF NOT EXISTS integration;  -- webhook endpoints/subscriptions/deliveries/attempts, credentials
CREATE SCHEMA IF NOT EXISTS kiosk;        -- kiosks, devices, health, heartbeats, screen savers, locations
CREATE SCHEMA IF NOT EXISTS merchant;     -- merchants, franchise wallets, topup requests, ledger
CREATE SCHEMA IF NOT EXISTS monitor;      -- exception logs, api request logs, health checks
CREATE SCHEMA IF NOT EXISTS ops;          -- technicians, deployment/maintenance/collection tickets, snapshots
CREATE SCHEMA IF NOT EXISTS payment;      -- payments, methods, payouts, providers, refunds, cash dispense
CREATE SCHEMA IF NOT EXISTS pricing;      -- offers, lines, policies, metal rates, fx rates, payout matrix
CREATE SCHEMA IF NOT EXISTS store;        -- stores, locations, contacts, balances, operators
CREATE SCHEMA IF NOT EXISTS tenancy;      -- tenants, features, branding, configs, legal entities, reference data
CREATE SCHEMA IF NOT EXISTS tx;           -- transactions, items, events, photos, analyses, pawn loans
CREATE SCHEMA IF NOT EXISTS voucher;      -- vouchers, redemption policies, redemptions, promotional offers
CREATE SCHEMA IF NOT EXISTS reporting;    -- views + matviews (read-only schema)

\echo '  ✓ 19 schemas created'

-- ─── Per-schema comment so pg_dump captures intent ──────────────────────────

COMMENT ON SCHEMA audit       IS 'Append-only audit events, data access logs, outbox/inbox messaging.';
COMMENT ON SCHEMA compliance  IS 'GDPR/PDPL/PDPA data subject rights, retention policies, deletion jobs.';
COMMENT ON SCHEMA config      IS 'Feature flags (with overrides), application configuration, scheduled jobs.';
COMMENT ON SCHEMA customer    IS 'Customer master, KYC artefacts, customer wallets and ledger.';
COMMENT ON SCHEMA doc         IS 'Receipts, contracts, templates, signatures, customer statements.';
COMMENT ON SCHEMA helpdesk    IS 'Customer feedback, support tickets, SOS requests, ticket taxonomy.';
COMMENT ON SCHEMA identity    IS 'Internal users (staff/admins), MFA, sessions, RBAC.';
COMMENT ON SCHEMA integration IS 'Webhook subscriptions, deliveries, external integration credentials.';
COMMENT ON SCHEMA kiosk       IS 'Kiosk masters, peripheral devices, health/heartbeats, kiosk content.';
COMMENT ON SCHEMA merchant    IS 'Franchise merchants, merchant wallets, top-up requests, ledger.';
COMMENT ON SCHEMA monitor     IS 'Application exceptions, API request logs, synthetic health checks.';
COMMENT ON SCHEMA ops         IS 'Field operations: technicians, tickets (deployment/maintenance/collection), inventory snapshots.';
COMMENT ON SCHEMA payment     IS 'Payments, payout providers, attempts, refunds, cash dispense, cassettes.';
COMMENT ON SCHEMA pricing     IS 'Price offers, pricing policies, metal/FX rates, tier deductions, payout matrix.';
COMMENT ON SCHEMA store       IS 'Physical stores hosting kiosks; locations, contacts, balances, operators.';
COMMENT ON SCHEMA tenancy     IS 'Multi-tenancy root + global reference data (countries, currencies, languages).';
COMMENT ON SCHEMA tx          IS 'Customer transactions, items, analyses, weight/karat readings, pawn loans.';
COMMENT ON SCHEMA voucher     IS 'Promotional vouchers, redemption policies, redemptions, marketing offers.';
COMMENT ON SCHEMA reporting   IS 'Read-only views and materialised views aggregating data from other schemas.';

\echo '  ✓ schema comments set'

-- ─── Revoke PUBLIC defaults (lock down before granting per role in 0003) ────
-- By default Postgres grants USAGE on every schema to PUBLIC. Revoke that so
-- only the roles we explicitly grant in 0003 can use these schemas.

REVOKE ALL ON SCHEMA audit, compliance, config, customer, doc, helpdesk,
                     identity, integration, kiosk, merchant, monitor, ops,
                     payment, pricing, store, tenancy, tx, voucher, reporting
       FROM PUBLIC;

\echo '  ✓ PUBLIC revoked from all schemas'

-- ─── Verification ────────────────────────────────────────────────────────────
\echo ''
\echo '── Schemas (excluding system) ──'
SELECT n.nspname AS schema,
       pg_catalog.obj_description(n.oid, 'pg_namespace') AS comment
FROM pg_namespace n
WHERE n.nspname NOT IN ('pg_catalog', 'pg_toast', 'information_schema', 'public')
  AND n.nspname NOT LIKE 'pg_temp_%'
  AND n.nspname NOT LIKE 'pg_toast_temp_%'
ORDER BY n.nspname;

\echo ''
\echo '✓ 0002 complete.'
