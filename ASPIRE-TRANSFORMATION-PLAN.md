# GoldKiosk Platform Build Plan — .NET 10 + Aspire (T0–T14)

> Goal: stand up the new enterprise solution per CLAUDE.md §2, runnable/debuggable with one F5
> (Aspire dashboard + distributed tracing), shippable as App Service (cloud) + one MSIX (kiosk).
> Read CLAUDE.md first. Legacy `../goldkiosk-platform` is reference-only.

## Ports (fixed for dev — external tooling depends on these)

| Resource | Project | HTTPS port |
|---|---|---|
| cloud-api | GoldKiosk.Cloud.Api | 7101 |
| admin-portal | GoldKiosk.Cloud.AdminPortal | 7102 |
| crm-portal | GoldKiosk.Cloud.CrmPortal | 7103 |
| kiosk-api | GoldKiosk.Kiosk.Api | 7201 |
| kiosk-ui | GoldKiosk.Kiosk.UI | n/a (desktop) |

## Prerequisites

- .NET 10 SDK; Aspire templates: `dotnet new install Aspire.ProjectTemplates` then `dotnet new list aspire` to confirm exact template ids for the installed version.
- PostgreSQL 18 native local install with `goldkiosk_local` and `goldkiosk_crm_local`. Docker NOT required (connection-string resources; container Postgres only if explicitly requested).
- Windows SDK / MSIX tooling for the packaging project (T12).

## Tasks

### T0 — Repo & engineering baseline
Branch `feature/GK-0-bootstrap`. Create `GoldKiosk.slnx`, `Directory.Build.props`
(net10.0, LangVersion latest, Nullable enable, ImplicitUsings, TreatWarningsAsErrors,
AnalysisLevel latest-recommended, deterministic), `Directory.Packages.props` (central,
pinned), `.editorconfig` (encodes csharp-coding-standards), `.gitignore`, `.gitattributes`.

### T1 — ServiceDefaults + AppHost
```bash
dotnet new aspire-servicedefaults -n GoldKiosk.ServiceDefaults -o src/orchestration/GoldKiosk.ServiceDefaults
dotnet new aspire-apphost        -n GoldKiosk.AppHost         -o src/orchestration/GoldKiosk.AppHost
```
Add both to the slnx. ServiceDefaults content stays as generated initially (OTel, health,
resilience, service discovery). ServiceDefaults is referenced by **web** projects only —
never by Kiosk.UI/Devices/Diagnostics.

### T2 — Shared core
Create `GoldKiosk.Domain`, `GoldKiosk.Application`, `GoldKiosk.Infrastructure`,
`GoldKiosk.Contracts` (classlib, net10.0) under src/shared/. Wire references per the
dependency rule (architecture-principles §1). Seed Domain with `Money`, `Purity`,
`GoldWeight` value objects + `EntityBase` with domain events; test-writer covers them 100%.

### T3 — Cloud hosts
Create `GoldKiosk.Cloud.Api` (webapi, Minimal APIs), `GoldKiosk.Cloud.AdminPortal` and
`GoldKiosk.Cloud.CrmPortal` (Blazor Server or MVC — architect decides in the T3 design note).
Each: Npgsql legacy-timestamp switch at very top of Program.cs, `AddServiceDefaults()`,
`MapDefaultEndpoints()`, `UseSnakeCaseNamingConvention()`, fixed ports per table above.

### T4 — Kiosk edge projects
`GoldKiosk.Kiosk.Api` (webapi net10.0 — REST + SignalR hub for hardware events, local SQLite
or file-backed outbox store per architect note), `GoldKiosk.Kiosk.Core` (classlib),
`GoldKiosk.Kiosk.Devices` (classlib net10.0-windows, hardware adapters behind ports with
simulator implementations for dev), `GoldKiosk.Kiosk.UI` (Blazor Hybrid: BlazorWebView in
WPF shell, net10.0-windows), `GoldKiosk.Kiosk.Diagnostics` (console exe, net10.0-windows).
Harvest flow-coordinator + view-model *logic* from the legacy KioskApp as reference; re-author
views as Razor.

### T5 — Postgres in the AppHost (Option A — native, no Docker)
```csharp
var pg    = builder.AddConnectionString("DefaultConnection"); // goldkiosk_local
var pgCrm = builder.AddConnectionString("CrmConnection");     // goldkiosk_crm_local
```
Values live in **AppHost user-secrets** (print the commands; human runs them):
```bash
cd src/orchestration/GoldKiosk.AppHost
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=goldkiosk_local;Username=postgres;Password=<pw>"
dotnet user-secrets set "ConnectionStrings:CrmConnection"     "Host=localhost;Port=5432;Database=goldkiosk_crm_local;Username=postgres;Password=<pw>"
```

### T6 — Register resources
```csharp
var cloudApi = builder.AddProject<Projects.GoldKiosk_Cloud_Api>("cloud-api").WithReference(pg);

builder.AddProject<Projects.GoldKiosk_Cloud_AdminPortal>("admin-portal")
       .WithReference(pg).WithReference(cloudApi);

builder.AddProject<Projects.GoldKiosk_Cloud_CrmPortal>("crm-portal").WithReference(pgCrm);

var kioskApi = builder.AddProject<Projects.GoldKiosk_Kiosk_Api>("kiosk-api")
       .WithReference(cloudApi);

builder.AddProject<Projects.GoldKiosk_Kiosk_UI>("kiosk-ui")
       .WithReference(kioskApi);   // desktop resource: launches with F5, reads kiosk-api URL from env; no HTTP health expected
```
Internal calls may use service discovery (`http://cloud-api`); external tooling keeps the
fixed ports. Don't half-migrate.

### T7 — Secrets as Aspire parameters
```csharp
var claudeKey  = builder.AddParameter("claude-api-key", secret: true);
var goldApiKey = builder.AddParameter("gold-api-key",  secret: true);
cloudApi.WithEnvironment("Claude__ApiKey", claudeKey)
        .WithEnvironment("GoldApi__ApiKey", goldApiKey);
```
Set via `dotnet user-secrets set "Parameters:claude-api-key" "<key>"` etc. (human runs).
JWT signing key likewise when auth lands.

### T8 — Configuration layering (see configuration-and-operations standard)
Implement `GoldKiosk.Kiosk.Api` + `Kiosk.UI` config pipeline: per-machine
`%ProgramData%\GoldKiosk\config\kiosk-settings.json` → cloud config from Blob (cached, ETag)
→ Key Vault-backed secrets (via Cloud.Api config endpoint in dev; device identity in prod).
Cloud apps: standard appsettings + Key Vault provider. Options classes with
`ValidateDataAnnotations().ValidateOnStart()` for every section.

### T9 — Logging & telemetry (see configuration-and-operations standard)
Serilog everywhere. Kiosk: rolling files under the machine transaction folder
(`%ProgramData%\GoldKiosk\logs\...`), App Insights sink gated to Warning+ and whitelisted
`TelemetryEvents`. Cloud: App Insights + OTel via ServiceDefaults. No PII anywhere.

### T10 — Verify (human, in Visual Studio)
Startup project = GoldKiosk.AppHost → F5 → dashboard shows cloud-api, admin-portal,
crm-portal, kiosk-api Running/Healthy, kiosk-ui launched; logs stream; one request
admin-portal → cloud-api appears as a single trace; a kiosk action traces
kiosk-ui → kiosk-api → cloud-api; breakpoints hit in two services on one request;
per-project run profiles still work.

### T11 — Solution filters + docs
`KioskEdge.slnf` (AppHost, Kiosk.*, shared, their tests) and `Cloud.slnf` (AppHost, Cloud.*,
shared, their tests) at repo root. Update CLAUDE.md "Running with Aspire (F5)" details and
the `project_aspire_transformation` memory.

### T12 — MSIX packaging (see packaging-and-deployment standard)
`src/packaging/GoldKiosk.Kiosk.Package` (Windows Application Packaging project):
- Contains Kiosk.UI (entry app), Kiosk.Api (logon-launched full-trust process), Devices, Diagnostics exe.
- Package identity, signing config (cert via pipeline secret, never in repo), version stamped from CI.
- `deploy/appinstaller/GoldKiosk.Kiosk.appinstaller` template pointing at Blob/CDN, hours-based update check, critical-update flag.
- Kiosk.Api started by Kiosk.UI shell at logon (not a Windows service); watchdog + restart policy.

### T13 — Machine setup & diagnostics deliverables
- `GoldKiosk.Kiosk.Diagnostics`: hardware self-test (dispenser, scale, printer, camera, network, cloud reachability, clock drift, disk space), writes a signed report to the transaction folder and optionally uploads a summary event. Runnable by field techs from inside the MSIX install.
- `deploy/setup-docs/`: provisioning runbook template (Windows config, kiosk account, autologon + assigned access, cert install, `kiosk-settings.json` authoring, network allow-list, App Installer trust), generated/updated per release.

### T14 — CI seams (definition only, wiring later)
Document (docs/adr) the pipeline stages: build → format check → NUnit (CI runs tests; Claude never does) → pack MSIX (signed) → publish appinstaller to Blob → App Service deploy slots for cloud apps. Staged rollout rings + health-gated rollback driven from AdminPortal.

## Caveats & gotchas

- Npgsql legacy-timestamp switch stays at the very top of every Program.cs; ordering matters.
- ServiceDefaults never referenced by desktop/windows projects (UI, Devices, Diagnostics).
- Kiosk must degrade gracefully offline: local durability first, outbox forward, stale-price lockout — never a silent wrong price, never a lost transaction.
- CRM uses its own database resource (`pgCrm`); no cross-database joins.
- MSIX constraint: no Windows services; Kiosk.Api is logon-launched. Plan watchdog accordingly.
- Aspire is never deployed; nothing may take a runtime dependency on the AppHost.

## Definition of done (v1)

T10 passes end-to-end, kiosk edge in the graph, tests green in VS, MSIX project builds a
signed package locally, standards docs referenced from CLAUDE.md all satisfied.
