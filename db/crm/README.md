# GoldKiosk CRM — Database (PostgreSQL 18)

Self-contained PostgreSQL scripts for the **GoldKiosk CRM Admin Dashboard**. This is the
PostgreSQL port of the CRM schema, modelled on the partner Admin Dashboard's `db/` folder
(`../../db/`): **single database, many schemas**, numbered idempotent scripts, enterprise
roles, and row-level security driven by a per-request GUC.

One PG database per environment — `goldkiosk_crm_local`, `goldkiosk_crm_sit`,
`goldkiosk_crm_uat`, `goldkiosk_crm_prod` — with three domain-grouped schemas inside.

> **Scope note.** These scripts are a parallel *database* deliverable. The running ASP.NET
> Core app still targets hosted Supabase Postgres; nothing in `src/` was changed. This folder
> lets you stand the CRM schema up on plain PostgreSQL (local, Azure Flexible Server, RDS,
> on-prem) without Supabase. See **§ Divergences from the Supabase original** below.

## Folder contents

Files run in lexical order; the numeric prefix dictates sequence. All files are **idempotent**.

| Range | Concern | Files |
|---|---|---|
| `00xx` | Bootstrap — extensions + helpers, schemas, roles + grants (`0004`/`0005` are numbered no-ops; enum domains are now text + CHECK) | `0001` – `0005` |
| `001x` | Table DDL, grouped by schema | `0010` – `0012` |
| `01xx` | Context + RLS-helper functions | `0100` |
| `02xx` | Triggers (updated_at, append-only, business) | `0200` – `0202` |
| `03xx` | Business functions / RPCs | `0300` – `0303` |
| `04xx` | Reporting views | `0400` |
| `06xx` | Row-level security policies | `0600` |
| `07xx` | Final grants + SECURITY DEFINER owner reassignment | `0700` |
| `08xx` | Demo seed (local / SIT only — guarded by database name) | `0800` |

## Schemas

| Schema | Holds |
|---|---|
| `crm` | Core CRM: `profiles` (staff), `leads`, `lead_activities`, `partners`, `tenants`, `contracts`, `activation_keys`, `user_preferences`. Enumerated domains are `text` columns + `CHECK` constraints (no native enum types). |
| `billing` | `subscriptions`, `subscription_periods`, `renewal_reminders`, `plan_catalog`, `stripe_event_log`, `ses_message_log`. |
| `audit` | `audit_log` — append-only compliance trail. |

## Roles (cluster-global — prefixed `gk_crm_` to avoid clashing with the Admin Dashboard's `gk_*`)

| Role | Privilege |
|---|---|
| `gk_crm_owner` | DDL on every schema. Humans use this during deployments. |
| `gk_crm_app` | DML on every schema; **subject to RLS**. The web app connects as this. |
| `gk_crm_readonly` | SELECT only. For BI / reporting tools. |
| `gk_crm_backend` | DML + **BYPASSRLS**. Background jobs (Stripe webhook, SES sender, cron) and the owner of every `SECURITY DEFINER` function. Replaces Supabase's `service_role`. |

## Row-level security model

Supabase's RLS read `auth.uid()` from the request JWT. Plain PostgreSQL has no such thing, so
this port uses the same GUC pattern as the Admin Dashboard: the app sets the signed-in user id
once per request, and policies read it back.

```sql
-- once per request, right after BEGIN, as gk_crm_app:
SELECT crm.set_user_context('<profile-uuid>');

-- policies then evaluate against:
crm.current_user_id()      -- the GUC, as uuid
crm.current_user_role()    -- that user's crm_role (sales_manager | sales_rep)
crm.is_sales_manager()     -- convenience boolean
crm.is_crm_user()          -- convenience boolean
```

> **GUC contract (important — see fix #9 below).** `set_user_context` sets `app.user_id`
> **LOCAL** (transaction-scoped). The app **must** call it as the first statement *inside*
> the request transaction and run its queries in that same transaction. Called as a
> stand-alone autocommit statement the value evaporates the moment the call returns and
> every RLS check then sees `current_user_id() = NULL` (all access silently denied). It is
> never session-scoped: on a pooled connection that would leak one request's identity into
> the next. `crm.reset_user_context()` clears it (also LOCAL).

Scoping (unchanged from the Supabase schema): **`sales_manager` sees everything**;
**`sales_rep` sees their own leads + the partners/tenants/contracts/subscriptions that descend
from those leads** (`partners.lead_id` joined to a lead they own). `gk_crm_backend` bypasses RLS
entirely. `gk_crm_readonly` is granted a permissive `SELECT` on every table (internal BI; the
CRM is single-org, not multi-tenant).

## Apply locally

Prerequisites:
- PostgreSQL 18 running on `localhost:5432`, `psql` on PATH (`C:\Program Files\PostgreSQL\18\bin`).
- Database created: `psql -U postgres -c "CREATE DATABASE goldkiosk_crm_local;"`

Then apply everything in order:

```powershell
cd "D:\Claude\goldkiosk\goldkiosk-software\goldkiosk-software-latest\goldkiosk-platform\db\crm"
./apply-all.ps1 -Database goldkiosk_crm_local
```

Or apply a single file:

```powershell
psql -U postgres -h localhost -d goldkiosk_crm_local -f 0001_bootstrap_extensions.sql -v ON_ERROR_STOP=1
```

To bring up a SIT/RDS database instead of local, the CRM build/deploy helpers live under
`../../scripts/crm/` (and the Admin Dashboard's under `../../scripts/admin-dashboard/`).

The demo seed in `0800` only fires when the database name starts with `goldkiosk_crm_local` or
`goldkiosk_crm_sit`, so applying the same set to UAT/Prod leaves the business tables empty.

## Conventions (same as the Admin Dashboard)

| Object | Pattern | Example |
|---|---|---|
| Schema | lowercase, domain-grouped | `crm`, `billing`, `audit` |
| Table | plural `snake_case` | `crm.leads`, `billing.subscriptions` |
| Column | singular `snake_case` | `tenant_id`, `created_at`, `mrr_amount` |
| Primary key | `id uuid PRIMARY KEY DEFAULT gen_random_uuid()` (or `GENERATED ALWAYS AS IDENTITY` for logs) | |
| Constraint | `pk_<t>`, `fk_<t>__<ref>`, `uq_<t>__<cols>`, `ck_<t>__<purpose>` | `fk_partners__leads` |
| Index | `ix_<t>__<cols>` (non-unique), `ux_<t>__<cols>` (unique) | `ix_leads__stage` |
| Trigger | `tg_<t>__<event>_<purpose>` | `tg_leads__au_activity_on_stage_change` |
| Function | `<schema>.<verb>_<noun>` | `crm.move_lead_stage` |
| View | `<schema>.v_<noun>` | `billing.v_renewals_upcoming` |

**Currency contract:** every monetary table has `currency_code text NOT NULL DEFAULT 'USD'` with a
`CHECK` constraint allowing `USD, AED, INR, EUR, GBP, SAR, QAR, KWD, BHD, OMR`. Render `{code} {amount:N0}`.

## Divergences from the Supabase original

The source migrations live at
`../../designs/goldkiosk-crm-dashboard/supabase/migrations/`. This port is the *consolidated
final state* of that chain, reorganised to the Admin Dashboard's layout, with these deliberate
changes:

1. **No `auth.users`.** `crm.profiles` is self-contained (its own `id` PK + optional
   `password_hash`/`last_signed_in_at`). The Supabase `auth.users -> profiles` sync trigger is
   gone; in production, authentication is external (Entra ID / app-managed).
2. **`auth.uid()` -> `crm.current_user_id()`**, and the `is_sales_manager()` / `is_crm_user()`
   helpers move to the `crm` schema and read the GUC.
3. **Supabase roles -> enterprise roles.** `authenticated -> gk_crm_app`,
   `service_role -> gk_crm_backend`, `anon` dropped.
4. **No `supabase_realtime` publication and no `storage.*` policies** — both are Supabase-only.
   Realtime/contract-PDF storage are an application concern, not a schema concern, on plain PG.
5. **Offline Ed25519 licensing is the final state.** The early `provision_tenant` /
   `reissue_activation_key` / `consume_activation_key` RPCs were dropped upstream; this port
   ships only `crm.ensure_tenant_for_partner`, `crm.record_issued_license`, and
   `crm.revoke_license_key`. The C# `LicenseSigner` mints the cleartext token + hash; the DB
   stores only the hash.
6. **No native enums** — every former enum domain (Supabase used native enum types) is a plain
   `text` column with a `CHECK (... IN (...))` constraint (named `ck_<table>__<col>`), matching the
   sibling GoldKiosk.AdminDashboard schema. This lets the EF Core / Npgsql layer map every such
   column to a C# `string` with no enum mapping. The allowed values for each former enum are listed
   in `0004_enums.sql` (now a numbered no-op kept for ordering).

## Post-review hardening (2026-06-16)

The first push of this folder was reviewed and the following correctness/security issues
were fixed. All are verified by a clean-room apply of `0001`–`0800` plus a functional test
pass on PostgreSQL 18. None change a table's column shape, so they are safe to re-apply over
an existing database.

| # | Sev | File | Fix |
|---|---|---|---|
| 3 | High | `0300_functions_pipeline.sql` | `crm.move_lead_stage` **and** `crm.mark_lead_won_and_create_partner` now re-assert ownership (sales_manager **or** lead owner) before mutating. These run as `gk_crm_backend` (BYPASSRLS), so without the check any CRM user could stage/convert any lead — a lead-hijack chain. |
| 4 | High | `0600_rls_policies.sql` | Direct `crm.partners` INSERT by `gk_crm_app` is now **denied** (restrictive `false`) — partners are created only via the audited `mark_lead_won_and_create_partner` RPC. `crm.contracts` INSERT is **scoped** to partners the caller can access (was `is_crm_user()`, which let a rep plant contracts against any partner). |
| 5 | High | `0302_functions_billing.sql` | `renew_subscription` keeps its `IF NOT FOUND` guard (a missing subscription raises instead of writing NULLs). |
| 6 | High | `0302_functions_billing.sql` | `renew_subscription` is now **idempotent on `external_invoice_id`**: a duplicate (Stripe at-least-once retry) returns the current period end without advancing the period or inserting a duplicate. |
| 7 | Med | `0303_functions_integrations.sql` | `stripe_apply_invoice_paid` now **claims the Stripe event in the same transaction** as the renewal (`INSERT … ON CONFLICT (stripe_event_id) DO NOTHING`). A crash rolls back both, so the retry recovers; a true duplicate is a no-op. **Signature changed** — the webhook handler now passes `(stripe_event_id, stripe_subscription_id, stripe_invoice_id, stripe_payment_id, paid_at[, event_type, api_version, payload])` and must **not** pre-insert the event for `invoice.paid`. |
| 8 | Med | `0302_functions_billing.sql` | New `billing.add_billing_cycle(from, cycle)` helper replaces the no-`ELSE` `CASE`s in `start_subscription`/`renew_subscription`; it **raises** on a NULL/unsupported cycle instead of letting NULL flow into the NOT NULL `current_period_end`. |
| 9 | Med | `0100_functions_context.sql` | `set_user_context` documents and enforces the **LOCAL/transaction** contract (see the GUC box above); added `crm.reset_user_context()`. |
| 10 | Med | `0600_rls_policies.sql` | `leads_app_update`/`leads_app_delete` USING now require `deleted_at IS NULL`, so the app role cannot edit or resurrect soft-deleted leads. (Soft-delete/purge are `gk_crm_backend` operations — a future archive RPC.) |

> **Scripts:** the deploy/build scripts under `../../scripts/` were also fixed —
> `scripts/admin-dashboard/build-eb-bundle.ps1` had been copied verbatim from the CRM script
> (wrong project), both build scripts and `apply-migrations.ps1` had a `$repoRoot` that was one
> level too shallow, and `apply-migrations.ps1` pointed at the pre-restructure `db\apply-all.ps1`.
> See `../../scripts/README.md`.

## See also

- `../../db/` — the partner Admin Dashboard's database (different schema; the structural template).
- `../../designs/goldkiosk-crm-dashboard/supabase/migrations/` — the original Supabase chain.
- `../MEMORY.md`, `../CLAUDE.md` — CRM decisions, gotchas, and the live SIT deployment notes.
