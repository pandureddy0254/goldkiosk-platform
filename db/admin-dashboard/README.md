# GoldKiosk Admin Dashboard — Database (PostgreSQL 18)

Self-contained PostgreSQL schema for the GoldKiosk Admin Dashboard. **One database, many schemas** — a single Postgres database per environment (`goldkiosk_local`, `goldkiosk_sit`, `goldkiosk_uat`, `goldkiosk_prod`) with 19 domain-grouped schemas inside.

This is the source of truth for the data model. The .NET app expects these tables to exist.

---

## Quickstart — get the DB running locally

### Prerequisites

- **PostgreSQL 18** installed and running on `localhost:5432` (Windows installer from postgresql.org or the EDB build).
- `psql` on your `PATH` (typically `C:\Program Files\PostgreSQL\18\bin` on Windows).
- The default `postgres` superuser account, plus its password.

### 1. Create the database

```powershell
$env:PGPASSWORD = "<your-postgres-password>"
psql -U postgres -h localhost -c "CREATE DATABASE goldkiosk_local;"
```

### 2. Apply all migrations in order

```powershell
cd db
./apply-all.ps1 -Database goldkiosk_local
```

This runs every `*.sql` file in lexical order with `ON_ERROR_STOP=1` — the first failure aborts. Idempotent: safe to re-run.

To apply a single file:

```powershell
psql -U postgres -h localhost -d goldkiosk_local -v ON_ERROR_STOP=1 -f 0001_bootstrap_extensions.sql
```

### 3. Verify the seed data

```powershell
psql -U postgres -h localhost -d goldkiosk_local -c `
"SELECT 'tenants' tbl, count(*) FROM tenancy.tenants
 UNION ALL SELECT 'permissions', count(*) FROM identity.permissions
 UNION ALL SELECT 'role_templates', count(*) FROM identity.role_templates
 UNION ALL SELECT 'roles', count(*) FROM identity.roles;"
```

Expected:

| tbl | count |
|---|---|
| tenants | ≥ 1 (created by app on first run) |
| permissions | 38 |
| role_templates | 6 |
| roles | 0 (created per tenant by `identity.apply_system_roles`) |

After the .NET app starts for the first time, `DevStartupSeeder` will populate:

- 2 tenants: `GK-DEV` (00000000-0000-0000-0000-000000000001) and `EGB-AE-001` (Emirates Gold Bank)
- 1 admin user: `admin@goldkiosk.local` / `ChangeMe!123` (Owner on GK-DEV)
- 1 activation key: `AIKI-7F2A-9C31-B4E8` issued to `cio@emiratesgold.ae` (EGB-AE-001)
- 6 system roles × 2 tenants = 12 roles via `identity.apply_system_roles`

### 4. Tell the .NET app about the database

In the project root, add the connection string to `dotnet user-secrets`:

```powershell
cd GoldKiosk.AdminDashboard
dotnet user-secrets set "ConnectionStrings:Default" `
  "Host=localhost;Port=5432;Database=goldkiosk_local;Username=postgres;Password=<your-postgres-password>"
```

The connection string never goes into `appsettings*.json` — it lives in user-secrets so it stays out of source control.

### 5. Run the app

```powershell
dotnet run --launch-profile http
# → http://localhost:5007
```

Sign in as `admin@goldkiosk.local` / `ChangeMe!123`.

---

## Folder contents

Files are applied in lexical order; the numeric prefix dictates sequence.

| Range | Concern | Files |
|---|---|---|
| `000x` | Bootstrap — extensions, schemas, roles, grants | `0001` – `0003` |
| `001x` – `003x` | Table DDL, one file per schema | `0010` – `0033` |
| `02xx` | Triggers (audit, immutable, balance, updated_at) | `0200` – `0204` |
| `03xx` | Business functions (ledger, audit, pricing, compliance, tenancy) | `0300` – `0304` |
| `04xx` | Views + materialised views | `0400` – `0401` |
| `05xx` | Partitioning (pg_partman setup) | `0500` |
| `06xx` | Row-level security policies | `0600` |
| `07xx` | Final per-object grants | `0700` |
| `00xx` (tail) | Late additions — RBAC seed, activation-key funcs, RLS gaps | `0028` – `0034` |

All files are **idempotent** — safe to re-run any number of times.

---

## Apply to SIT / UAT / Prod

The same files run by the deployment pipeline (grate / DbUp / custom PowerShell job) against the env-specific connection string. Prod is gated by manual approval.

```powershell
./apply-all.ps1 -Database goldkiosk_sit -PgHost <hostname> -PgUser <principal>
```

---

## Conventions (enforced by file content)

| Object | Pattern | Example |
|---|---|---|
| Schema | lowercase, domain-grouped | `customer`, `tx`, `audit` |
| Table | plural `snake_case` | `customer.customers`, `tx.transaction_items` |
| Column | singular `snake_case` | `tenant_id`, `created_at`, `billed_weight_g` |
| Primary key | `id uuid PRIMARY KEY DEFAULT gen_random_uuid()` | |
| FK column | `<referenced_singular>_id` | `customer_id`, `tenant_id` |
| Constraint | `pk_<t>`, `fk_<t>__<ref>`, `uq_<t>__<cols>`, `ck_<t>__<purpose>` | `fk_transactions__customers` |
| Index | `ix_<t>__<cols>` non-unique, `ux_<t>__<cols>` unique | `ix_transactions__kiosk_id_occurred_at` |
| Trigger | `tg_<t>__<event>_<purpose>` | `tg_audit_events__bud_immutable` |
| Function | `<schema>.<verb>_<noun>` | `identity.validate_activation_key` |
| View | `<schema>.v_<noun>` | `reporting.v_daily_sales` |
| Matview | `<schema>.mv_<noun>` | `reporting.mv_daily_inventory_summary` |
| Domain | `domain_<noun>` in `public` | `domain_money`, `domain_karat` |

---

## Roles created (every environment)

| Role | Privilege | Used by |
|---|---|---|
| `gk_owner` | DDL on every schema | Deploy engineer running migrations |
| `gk_app` | DML on every schema; subject to RLS | The .NET app (production posture) |
| `gk_app_readonly` | `SELECT` only | BI / analytics tools |
| `gk_backend` | DML + `BYPASSRLS` | Background workers (relay, snapshots, retention) |
| `gk_audit_reader` | `SELECT` on `audit.*` + `compliance.*` | Compliance officers |

**Local dev caveat:** by default the app connects as the `postgres` superuser (BYPASSRLS=t), which means RLS is bypassed locally. For prod-equivalent isolation, create a `goldkiosk_app NOSUPERUSER NOBYPASSRLS LOGIN` role and update the user-secrets connection string to use it.

---

## Row-level security

Every tenant-scoped table has a `tenant_id` column and a `tenant_isolation` policy:

```sql
USING       (tenant_id = (current_setting('app.tenant_id', true))::uuid)
WITH CHECK  (tenant_id = (current_setting('app.tenant_id', true))::uuid);
```

The app sets `app.tenant_id` per request via `TenantContextInterceptor.ConnectionOpenedAsync`. If you open a connection manually, you **must** call `SetTenantContextAsync` — see `Services/Common/DbConnectionExtensions.cs`.

---

## Permission catalogue (38 codes)

Catalogued in `0032_seed_rbac_roles_and_permissions.sql`. Format: `module:verb` (e.g. `kiosks:write`, `customers:unmask`, `audit:export`). Six system roles seeded per tenant by `identity.apply_system_roles(tenant_id)`:

- `owner` — all 38 permissions, one per tenant, cannot be deleted
- `manager` — day-to-day ops
- `operator` — floor staff
- `analyst` — read-everywhere + report export
- `support` — helpdesk + PII unmask (logged)
- `auditor` — audit log + reports read-only

---

## Migration history (latest first)

- `0034_close_rls_gaps.sql` — RLS on `identity.activation_keys` + `identity.user_invitations`
- `0033_activation_key_functions.sql` — SECURITY DEFINER `validate_activation_key` + `consume_activation_key`
- `0032_seed_rbac_roles_and_permissions.sql` — 38 permissions + 6 role templates + per-tenant bootstrap
- `0031_tables_activation_invites_api.sql` — activation_keys + user_invitations + partner_api_credentials
- `0030_tables_tx_reversal.sql` — transaction reversal tables for `ReverseTransaction`

Older migrations are listed in lexical order — see folder.

---

## See also

- `../README.md` — top-level repo overview
- `../CLAUDE.md` — engineering guide (RLS, EF Core SqlQuery trap, page chrome contract)
- `../.claude/rules.md` — binding engineering rules
- `../.claude/skills/postgres-migration/SKILL.md` — how to write a new migration
- `../.claude/agents/admin-db-migrator.md` — sub-agent for migration authoring
