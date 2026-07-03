# GoldKiosk Platform

Enterprise rebuild of the GoldKiosk / GoldCube precious-metals kiosk platform on **.NET 10**.
A customer walks up to a touchscreen kiosk, sells or pawns gold/silver jewellery — the machine
weighs it, XRF-analyses purity, verifies identity, generates an offer, and pays out. The
platform is a monorepo covering the kiosk edge (UI + local API + hardware), the cloud backend,
and the admin/CRM portals.

> **Status:** active build on branch `feature/GK-2-kiosk-vertical`. The kiosk vertical runs
> end-to-end in **mock (simulated-hardware) mode** and ships as a signed MSIX. Cloud.Api,
> AdminPortal, and CRMPortal are built and awaiting a local Postgres validation round.
> `main` holds only the bootstrap commit — all work lives on the feature branch.

---

## 1. Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | **10.0.301+** | `global.json` pins it. `dotnet --version` should report 10.x. |
| PostgreSQL | **18** | Cloud/portal databases. Service `postgresql-x64-18`; `psql` at `C:\Program Files\PostgreSQL\18\bin`. |
| Windows | 10.0.19041+ (x64) | Kiosk UI/Devices are `net10.0-windows`; MSIX packaging needs the Windows 10 SDK. |
| Windows 10 SDK | 10.0.19041 | `makeappx.exe` + `signtool.exe` for MSIX (`…\Windows Kits\10\bin\10.0.19041.0\x64`). |
| WebView2 Runtime | current | Ships with Edge; required by the Blazor Hybrid kiosk UI. |
| Aspire templates | 13.4.x | `dotnet new install Aspire.ProjectTemplates` (only needed to regenerate AppHost/ServiceDefaults). |

Clone and confirm the toolchain:

```powershell
git clone https://github.com/pandureddy0254/goldkiosk-platform.git
cd goldkiosk-platform
git switch feature/GK-2-kiosk-vertical
dotnet --version   # -> 10.0.30x
```

---

## 2. Solution structure

`GoldKiosk.slnx` (root). Naming: `GoldKiosk.<Tier>.<Component>` for tier projects,
`GoldKiosk.<Component>` for shared libraries.

```
src/
  orchestration/
    GoldKiosk.AppHost            # .NET Aspire dev orchestrator (dev-only, never deployed)
    GoldKiosk.ServiceDefaults    # OTel, health, resilience, service discovery, OpenAPI/Swagger defaults
  shared/
    GoldKiosk.Domain             # entities + value objects (Money, Purity, GoldWeight) + Assay (karat) — BCL only
    GoldKiosk.Application         # use cases / ports (seeded)
    GoldKiosk.Infrastructure     # EF Core + Npgsql, entities, RLS interceptor, Argon2id identity
    GoldKiosk.Contracts          # versioned wire DTOs (V1/) + SignalR contracts. References nothing.
  cloud/
    GoldKiosk.Cloud.Api          # kiosk-facing backend: auth, rates, offers, AI item check, transactions
    GoldKiosk.Cloud.AdminPortal  # ops/fleet/pricing/reconciliation dashboard (MVC)
    GoldKiosk.Cloud.CRMPortal    # partner onboarding, licensing, billing (MVC)
  kiosk/
    GoldKiosk.Kiosk.Api          # on-machine edge API: owns hardware, local durability, outbox (REST + SignalR)
    GoldKiosk.Kiosk.UI           # Blazor Hybrid kiosk front-end (BlazorWebView in a WPF shell)
    GoldKiosk.Kiosk.Core         # edge domain: session state machine, file store, pricing, cloud gateway
    GoldKiosk.Kiosk.Devices      # hardware ports + simulators + real drivers (per-device mock/real config)
  packaging/
    GoldKiosk.Kiosk.Package      # MSIX packaging (AppxManifest, build.ps1, install.ps1, tile assets)
tests/                           # NUnit + Moq + FluentAssertions (only stack). TestKit + per-project test projects.
db/                              # SQL schema — admin-dashboard/ and crm/ (numbered scripts + apply-all.ps1)
deploy/                          # appinstaller/ + setup-docs/ (provisioning runbook)
docs/                            # standards/ (binding), adr/, design/, legacy/, direction/
```

---

## 3. Build & verify

The solution is **warnings-as-errors** with analyzers and enforced formatting.

```powershell
dotnet build GoldKiosk.slnx -warnaserror        # must pass with 0 warnings
dotnet format GoldKiosk.slnx --verify-no-changes # formatting/style gate
```

**Do not run `dotnet test`** — tests are executed in Visual Studio / CI (see §8). Agents and
scripts verify with `dotnet build` only.

`.slnf` filters (planned) let you load focused subsets; until then, build a single project with
`dotnet build src/<path>/<Project>.csproj -warnaserror`.

---

## 4. Fixed dev ports

External tooling (Postman, the UI) depends on these.

| Resource | Project | HTTPS | HTTP |
|---|---|---|---|
| cloud-api | GoldKiosk.Cloud.Api | 7101 | 5101 |
| admin-portal | GoldKiosk.Cloud.AdminPortal | 7102 | 5102 |
| crm-portal | GoldKiosk.Cloud.CRMPortal | 7103 | 5103 |
| kiosk-api | GoldKiosk.Kiosk.Api | 7201 | 5201 |
| kiosk-ui | GoldKiosk.Kiosk.UI | desktop | — |

---

## 5. Run the kiosk (mock / offline — no cloud, no DB needed)

This is the fastest way to see a full transaction. All hardware is simulated; offers come from
the local mock calculator; nothing external is required.

```powershell
# Terminal 1 — the edge API (mock devices, port 5201)
dotnet run --project src/kiosk/GoldKiosk.Kiosk.Api --launch-profile http

# Terminal 2 — the kiosk UI (once the API answers)
dotnet run --project src/kiosk/GoldKiosk.Kiosk.UI
```

The UI reads `KioskUi:ApiBaseUrl` (default `http://localhost:5201`). Walk the flow:
attract → welcome → place item → analysis (SignalR-paced) → offer → identity → contact →
payout → done. Transaction folders are written under
`src/kiosk/GoldKiosk.Kiosk.Api/data/transactions/` (dev) or `%ProgramData%\GoldKiosk\` (packaged).
DEBUG exit gesture: **Esc+F12**.

Enable the cloud path (real offers + transaction upload) by setting `Cloud:Enabled=true` in the
Kiosk.Api config and running Cloud.Api (see §6/§7); with it `false` (default) the kiosk is
fully offline.

---

## 6. Run the cloud & portals

Each needs its Postgres database and secrets (see §7).

```powershell
dotnet run --project src/cloud/GoldKiosk.Cloud.Api          # https://localhost:7101 (Swagger at /swagger in Dev)
dotnet run --project src/cloud/GoldKiosk.Cloud.AdminPortal  # https://localhost:7102
dotnet run --project src/cloud/GoldKiosk.Cloud.CRMPortal    # https://localhost:7103
```

**Aspire one-F5 (planned):** `GoldKiosk.AppHost` will orchestrate all resources with a
dashboard + distributed tracing. Resource registration (T5–T6) is not wired yet; run projects
individually for now.

---

## 7. Configuration & secrets

**Secrets never live in committed files.** Dev uses `dotnet user-secrets`; production uses Key
Vault / device identity. Fixed identity/hardware facts live in config; secrets are supplied per
machine. The full key table is in `deploy/setup-docs/provisioning-runbook.md`.

### Database

```powershell
# Start Postgres (elevated) and create the dev databases
Start-Service postgresql-x64-18
$env:PGPASSWORD = '<your-postgres-password>'
$psql = 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
& $psql -U postgres -c "CREATE DATABASE goldkiosk_local;"
& $psql -U postgres -c "CREATE DATABASE goldkiosk_crm_local;"

# Apply the schema (numbered scripts, idempotent)
cd db/admin-dashboard; ./apply-all.ps1; cd ../..
cd db/crm;             ./apply-all.ps1; cd ../..
```

### User-secrets per project

```powershell
# Cloud.Api
dotnet user-secrets set "ConnectionStrings:goldkiosk" "Host=localhost;Port=5432;Database=goldkiosk_local;Username=postgres;Password=<pw>" --project src/cloud/GoldKiosk.Cloud.Api
dotnet user-secrets set "Jwt:Key"        "<random 32+ char key>" --project src/cloud/GoldKiosk.Cloud.Api
dotnet user-secrets set "GoldApi:ApiKey" "<goldapi.io key>"      --project src/cloud/GoldKiosk.Cloud.Api

# AdminPortal
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=goldkiosk_local;Username=postgres;Password=<pw>" --project src/cloud/GoldKiosk.Cloud.AdminPortal

# CRMPortal (generate a FRESH Ed25519 keypair — the legacy one is compromised)
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=goldkiosk_crm_local;Username=postgres;Password=<pw>" --project src/cloud/GoldKiosk.Cloud.CRMPortal
dotnet user-secrets set "License:ActiveKid" "<kid>" --project src/cloud/GoldKiosk.Cloud.CRMPortal
dotnet user-secrets set "License:Keys:<kid>:PrivateKeyBase64" "<base64 Ed25519 private key>" --project src/cloud/GoldKiosk.Cloud.CRMPortal
dotnet user-secrets set "License:Keys:<kid>:PublicKeyBase64"  "<base64 Ed25519 public key>"  --project src/cloud/GoldKiosk.Cloud.CRMPortal
dotnet user-secrets set "Ses:Region" "<region>" --project src/cloud/GoldKiosk.Cloud.CRMPortal
dotnet user-secrets set "Ses:AccessKeyId" "<id>"     --project src/cloud/GoldKiosk.Cloud.CRMPortal
dotnet user-secrets set "Ses:SecretAccessKey" "<key>" --project src/cloud/GoldKiosk.Cloud.CRMPortal

# Kiosk.Api cloud integration (only when Cloud:Enabled=true)
dotnet user-secrets init --project src/kiosk/GoldKiosk.Kiosk.Api
dotnet user-secrets set "Cloud:KioskPin" "<pin>" --project src/kiosk/GoldKiosk.Kiosk.Api
```

Config sections of note: `Devices` (per-device `Mode: Real|Mock` + `Overrides`), `Kiosk`
(KioskId/StoreId/TransactionRoot/timeouts), `MockRates`, `Cloud` (`Enabled`, `BaseUrl`,
`KioskCode`). See the standards docs for the full layering model.

---

## 8. Tests

Stack is **NUnit 4 + Moq + FluentAssertions (7.x, free line) only** — xUnit/MSTest/NSubstitute/
Shouldly are prohibited. Projects live under `tests/` mirroring the source namespaces; shared
builders/fakes in `GoldKiosk.TestKit`.

Run them in **Visual Studio Test Explorer** or CI (`dotnet test` in the pipeline). Locally,
verify only that they compile: `dotnet build tests/<Project>/<Project>.csproj -warnaserror`.

---

## 9. Build & install the kiosk MSIX

One signed package contains the UI (entry app) + the edge API (launched and supervised by the
UI shell) + devices. It installs to `C:\Program Files\WindowsApps\…` (read-only); runtime data
goes to `%ProgramData%\GoldKiosk\`.

```powershell
cd src/packaging/GoldKiosk.Kiosk.Package

# Publish both apps self-contained, pack, and sign with a dev self-signed cert
powershell -ExecutionPolicy Bypass -File build.ps1 -Version 0.2.0.0

# Trust the dev cert (machine store) and install — MUST run elevated
powershell -ExecutionPolicy Bypass -File install.ps1 -Version 0.2.0.0
```

The package auto-starts the kiosk at logon and shows a branded splash ("GoldKiosk App is ready")
while the edge API comes up. Launch manually with:
`explorer.exe shell:AppsFolder\GoldKiosk.Kiosk_75ym4yfw166m0!GoldKioskKiosk`.
Production packages are pipeline-signed with an EV cert (never in the repo).

---

## 10. Conventions (binding — see `docs/standards/`)

- **Branch first.** Never commit to `main`. `feature/<ticket>-<slug>`.
- **Conventional commits** (`feat:`/`fix:`/`test:`/`chore:`/`docs:`). No AI attribution in messages.
- **.NET 10 / C# 14**, file-scoped namespaces, nullable, `TimeProvider` (never `DateTime.Now`),
  `Money` value object for currency (never float/double), Result pattern for expected failures.
- **PostgreSQL**: snake_case, RLS via the `app.tenant_id` interceptor, Npgsql legacy-timestamp
  switch as the first line of every host's `Program.cs`.
- **No PII/secrets in logs**; PII never sent to third-party APIs unmasked.

Binding standards: `docs/standards/{csharp-coding,architecture-principles,testing,security,configuration-and-operations,packaging-and-deployment}.md`.
Decision records: `docs/adr/`. Design notes: `docs/design/`. Legacy analysis: `docs/legacy/`.

---

## 11. API references

- Kiosk edge API: `docs/api/kiosk-api-payload-samples.md` +
  `docs/api/GoldKiosk-Kiosk-Api.postman_collection.json` (import into Postman).
- Cloud.Api: Swagger UI at `https://localhost:7101/swagger` (Development only).
