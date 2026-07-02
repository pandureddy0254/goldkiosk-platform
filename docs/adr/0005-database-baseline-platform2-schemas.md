# ADR 0005 — Database baseline: adopt platform2 schema sets with enumerated fixes

Date: 2026-07-02
Status: Accepted (owner decision, 2026-07-02)

## Context

The previous platform attempt (`../goldkiosk-platform2/db`) contains two mature
PostgreSQL schema sets: `admin-dashboard` (50 scripts: tenancy, identity/RBAC, audit,
store, kiosk fleet, customer/KYC, transactions (event-sourced), pricing, payment, docs,
ops, monitor, helpdesk, merchant, voucher, integration, config, compliance, reporting)
and `crm` (26 scripts: pipeline, partners/tenants, licensing, billing/Stripe, audit).
The owner directed that their best parts be carried forward. Full analysis:
`docs/legacy/legacy-parity-analysis.md` §9. Notably, the schemas are already
thin-client-shaped — nothing assumes a kiosk-local database — matching ADR 0002/0003.

## Decision

- The platform2 schema sets are the **baseline** for the new databases:
  `admin-dashboard` → **`goldkiosk`** (platform DB behind Cloud.Api/AdminPortal);
  `crm` → **`goldkiosk_crm`** (behind CrmPortal). Same cluster, separate databases, no
  cross-database joins; the only contracts between them are the activation-key hash
  handshake and the `admin_tenant_id` soft pointer.
- Kept as-is (the "best parts"): schema-per-domain layout; numbered idempotent script
  bands + lexical apply order; five-role posture with FORCE RLS; custom domains
  (`domain_money`, `domain_karat`, …); text+CHECK instead of native enums; append-only +
  immutability triggers; ledger→balance materialization; transactional outbox + global
  idempotency inbox; event-sourced `tx.transaction_events` + snapshots; PII `*_enc` +
  blind-index `*_lookup_hash` pattern; RBAC template expander; hash-only activation keys;
  partitioned heartbeats/events/api-logs; CRM's per-operation + RESTRICTIVE-deny RLS with
  DEFINER-RPC-only write paths.
- Fixed on adoption (defects found in analysis):
  1. Tenant isolation for child tables lacking `tenant_id` (add the column or
     EXISTS-join policies) — customer/tx/payment/KYC/offer children.
  2. RLS becomes the final structural step and is idempotently re-runnable, so
     late-added tables can never miss policies (legacy `0802`/`0803` bug).
  3. Reference tables with `tenant_id IS NULL` global defaults get
     `OR tenant_id IS NULL` visibility (legacy silent-disappearance bug).
  4. GUC casts guarded: `NULLIF(current_setting('app.tenant_id', true), '')::uuid`.
  5. `audit.audit_events` partitioned; audit capture excludes large/encrypted columns.
  6. Redundant `0034` folded away; `reporting.expenses_source` placeholder resolved or
     dropped.
- Added for the thin-client kiosk model: kiosk **command queue** (cloud→kiosk with acks),
  kiosk **applied-config-version** tracking, and idempotency keys on kiosk-originated
  events (transactions already carry them via ADR 0002).
- Scripts land in this repo under `db/` following the same numbering-band convention;
  EF Core maps via snake_case conventions; SECURITY DEFINER RPCs are called through a
  thin parameterized-SQL layer, not LINQ.

## Consequences

- Months of schema design are reused; the new build starts from reviewed, convention-
  aligned DDL instead of greenfield tables.
- The fixes are deliberate deltas — reviews can diff against platform2 to see exactly
  what changed and why.
- The legacy MySQL (`be_*`) model is a capability reference only; data migration from
  MySQL, if ever needed, is a separate decision.
- pg_partman and citext become environment prerequisites; provisioning docs must include
  them.
