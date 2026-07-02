# Kickoff prompts — GoldKiosk enterprise build

Paste into a Claude Code session **started in this folder** so CLAUDE.md (and the imported
standards) auto-load. Run in order; each is a self-contained chunk. `/agents` should show
architect, developer, test-writer, code-reviewer before you start.

---

## Prompt 1 — Baseline + orchestration (T0–T1)

```
Read CLAUDE.md and ASPIRE-TRANSFORMATION-PLAN.md fully. Repeat the hard rules back to me,
then execute T0 and T1:
- branch feature/GK-0-bootstrap,
- GoldKiosk.slnx, Directory.Build.props (net10.0, warnaserror, analyzers, nullable),
  Directory.Packages.props (central, pinned), .editorconfig per the coding standards,
  .gitignore/.gitattributes,
- GoldKiosk.ServiceDefaults + GoldKiosk.AppHost under src/orchestration/, added to the slnx.
Build with dotnet build GoldKiosk.slnx -warnaserror. Do NOT run dotnet test.
Show me Directory.Build.props and the AppHost csproj before committing.
```

## Prompt 2 — Shared core with Domain seed (T2)

```
Do T2. Use the architect agent first for a one-page design note on the shared core
(Domain/Application/Infrastructure/Contracts references, Money/Purity/GoldWeight invariants,
rounding policy for INR, domain-event base). Stop for my approval. Then developer implements,
test-writer covers the value objects at 100% with NUnit + Moq + FluentAssertions per the
testing standard, code-reviewer reviews the diff. Build only; I run tests in Visual Studio.
```

## Prompt 3 — Cloud hosts (T3, T5–T6 cloud part)

```
Do T3 plus the cloud portion of T5–T6: create Cloud.Api, Cloud.AdminPortal, Cloud.CrmPortal
with the Npgsql legacy-timestamp switch, snake_case convention, AddServiceDefaults(),
MapDefaultEndpoints(), fixed ports 7101/7102/7103; AppHost connection-string resources
DefaultConnection + CrmConnection from AppHost user-secrets (print the exact user-secrets
commands for me — never set them yourself); register cloud-api, admin-portal (refs pg +
cloud-api), crm-portal (refs pgCrm). Build. Then give me the Visual Studio steps to set
GoldKiosk.AppHost as startup project.
```

## Prompt 4 — Kiosk edge (T4, T6 edge part)

```
Do T4 and the edge part of T6. Architect design note first (STOP for approval): Kiosk.Api
surface (REST + SignalR hub), local durability + outbox design, Devices port interfaces with
simulators, Kiosk.UI Blazor Hybrid shell that launches/monitors Kiosk.Api, Diagnostics exe
skeleton. Then implement, test (NUnit — outbox idempotency, stale-price lockout, simulator
contract tests), review. kiosk-api on port 7201; kiosk-ui as a desktop resource reading the
kiosk-api URL. Build only.
```

## Prompt 5 — Secrets, config layering, logging (T7–T9)

```
Do T7–T9 per the configuration-and-operations standard:
- Aspire secret parameters claude-api-key / gold-api-key -> Claude__ApiKey / GoldApi__ApiKey
  on cloud-api (print user-secrets commands, confirm nothing secret is committed),
- kiosk config pipeline: %ProgramData%\GoldKiosk\config\kiosk-settings.json + Blob-cached
  cloud config + Key Vault-backed secrets via the Cloud.Api config endpoint (device identity
  later), with validated Options classes,
- Serilog: kiosk rolling files under the transaction folder, App Insights sink gated to
  Warning+ and the TelemetryEvents whitelist; cloud apps App Insights + OTel.
Tests for: config precedence, options validation failure at startup, telemetry gate.
Build only. Show me the config-key table you added to the standard.
```

## Prompt 6 — Verify (T10) — I run this in Visual Studio

```
Give me the precise T10 verification checklist for Visual Studio: expected Aspire dashboard
resources/health/logs, the two distributed traces to look for (admin-portal -> cloud-api and
kiosk-ui -> kiosk-api -> cloud-api), how to confirm breakpoints hit in two services on one
request, and that per-project run profiles still work.
```

## Prompt 7 — Filters + docs (T11)

```
Do T11: KioskEdge.slnf and Cloud.slnf at repo root, refresh the "Running with Aspire (F5)"
notes in CLAUDE.md, update the project_aspire_transformation memory with the outcome.
```

## Prompt 8 — MSIX package (T12)

```
Do T12 per the packaging-and-deployment standard. Architect note first (STOP for approval):
GoldKiosk.Kiosk.Package WAP project containing Kiosk.UI (entry), Kiosk.Api (full-trust
logon-launched), Devices, Diagnostics; manifest identity/capabilities; version stamping;
signing via pipeline secret placeholder (no cert in repo); the .appinstaller template in
deploy/appinstaller pointing at Blob/CDN with update-check policy. Then implement and confirm
a local unsigned package build succeeds. Do not touch CI.
```

## Prompt 9 — Diagnostics + machine setup docs (T13)

```
Do T13: implement GoldKiosk.Kiosk.Diagnostics self-test (dispenser/scale/printer/camera via
the Devices simulators, network + cloud reachability, clock drift, disk space) writing a
report to the transaction folder and emitting one whitelisted telemetry summary event; and
generate deploy/setup-docs/provisioning-runbook.md (kiosk account, autologon + assigned
access, cert trust, kiosk-settings.json authoring guide with the full key table, network
allow-list, App Installer trust). NUnit tests for the self-test result aggregation.
```

---

**Guardrails reminder (also in CLAUDE.md):** branch first; never `dotnet test` (user tests in
VS); secrets via user-secrets/Key Vault only — agents print commands, never values; Npgsql
legacy-timestamp + snake_case locked; Aspire dev-only, never deployed; MSIX = UI + Kiosk.Api
combined, logon-launched, no Windows services; PII never in logs or LLM calls.
