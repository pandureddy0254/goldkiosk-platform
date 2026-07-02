# CLAUDE.md — GoldKiosk Platform (Enterprise Build)

> Auto-loaded when a Claude Code session starts in this folder. This is the constitution for
> building the new enterprise GoldKiosk solution. Read `ASPIRE-TRANSFORMATION-PLAN.md` next,
> then drive work with `KICKOFF-PROMPTS.md`. The standards below are **binding**, not advisory.

@docs/standards/csharp-coding-standards.md
@docs/standards/architecture-principles.md
@docs/standards/testing-standards.md
@docs/standards/security-standards.md
@docs/standards/configuration-and-operations.md
@docs/standards/packaging-and-deployment.md

---

## 1. Mission

Build the new GoldKiosk platform on **.NET 10** with **.NET Aspire** dev orchestration:
one F5 launches everything with a dashboard, cross-service breakpoints, and distributed
tracing. Production ships as **Azure App Service** (cloud apps) and **one signed MSIX**
containing the Kiosk UI + Kiosk Api + diagnostics tooling (edge).

The legacy `goldkiosk-platform` repo is **reference material only**: harvest business rules,
flow coordinators, SQL, and test scenarios from it — never copy code that violates the
standards docs.

## 2. Solution structure & naming (canonical — do not invent alternatives)

Naming convention: `GoldKiosk.<Tier>.<Component>` for tier-specific projects,
`GoldKiosk.<Component>` for shared libraries. Tiers: `Cloud` (Azure-hosted) and
`Kiosk` (edge, ships on machines).

```
GoldKiosk.slnx
Directory.Build.props            # net10.0, warnings-as-errors, analyzers, nullable
Directory.Packages.props         # central package management, pinned versions
src/
  orchestration/
    GoldKiosk.AppHost/                 # Aspire orchestrator (dev-only, NEVER deployed)
    GoldKiosk.ServiceDefaults/         # OTel, health, resilience, service discovery
  shared/
    GoldKiosk.Domain/                  # entities, aggregates, value objects (Money, Purity, GoldWeight)
    GoldKiosk.Application/             # use cases, ports, validators, pipeline behaviors
    GoldKiosk.Infrastructure/          # EF Core + Npgsql, Key Vault, Blob, external APIs
    GoldKiosk.Contracts/               # versioned wire DTOs (edge <-> cloud). References nothing.
  cloud/
    GoldKiosk.Cloud.Api/               # shared cloud backend: offers, AI, KYC, payout, config service
    GoldKiosk.Cloud.AdminPortal/       # admin dashboard (ops, kiosk fleet, pricing, reconciliation)
    GoldKiosk.Cloud.CrmPortal/         # CRM dashboard (customers, KYC review, campaigns, support)
  kiosk/
    GoldKiosk.Kiosk.Api/               # on-machine edge API: owns hardware, local store, outbox
    GoldKiosk.Kiosk.UI/                # Blazor Hybrid kiosk front-end (BlazorWebView shell)
    GoldKiosk.Kiosk.Devices/           # hardware SDK adapters (dispenser, scale, printer, camera…)
    GoldKiosk.Kiosk.Core/              # edge-local domain: transaction queue, staleness policy
    GoldKiosk.Kiosk.Diagnostics/       # self-test console/WinForms exe shipped inside the MSIX
  packaging/
    GoldKiosk.Kiosk.Package/           # Windows Application Packaging (MSIX) project
tests/                                 # NUnit only — see testing standards
  GoldKiosk.Domain.Tests/
  GoldKiosk.Application.Tests/
  GoldKiosk.Cloud.Api.Tests/
  GoldKiosk.Cloud.AdminPortal.Tests/
  GoldKiosk.Cloud.CrmPortal.Tests/
  GoldKiosk.Kiosk.Api.Tests/
  GoldKiosk.Kiosk.Core.Tests/
  GoldKiosk.IntegrationTests/
  GoldKiosk.TestKit/                   # shared builders, object mothers, fakes
deploy/
  appinstaller/                        # .appinstaller template + rollout docs
  setup-docs/                          # machine provisioning runbooks (generated per release)
db/                                    # SQL scripts, RLS policies, seed data
docs/
  standards/                           # binding standards (imported above)
  adr/                                 # architecture decision records
.claude/                               # settings, agents, commands (committed)
```

Namespaces mirror project names exactly. Resource names in the AppHost are kebab-case:
`cloud-api`, `admin-portal`, `crm-portal`, `kiosk-api`, `kiosk-ui`.

## 3. Locked decisions (do not re-open without asking)

- **.NET 10 / C# 14** everywhere (`net10.0`; `net10.0-windows` only for Kiosk.UI, Devices, Diagnostics, Package).
- **Frontend (kiosk) = Blazor Hybrid** — BlazorWebView in a WPF shell. Hardware lives behind `GoldKiosk.Kiosk.Api` (REST + SignalR), never in-process with the UI.
- **Two APIs**: `Kiosk.Api` on the machine owns hardware + local durability; `Cloud.Api` is the shared backend. All edge→cloud writes are idempotent via the outbox.
- **Edge decisions locked 2026-07-02** (see `docs/adr/`): no kiosk-local SQL — durability is file-based transaction folders in the legacy layout (ADR 0002); kiosks talk exclusively to `Cloud.Api`, never CRM (ADR 0003); every device selects mock/real per device via layered config, cloud overrides local (ADR 0004); DB baseline = platform2 schema sets with enumerated fixes (ADR 0005).
- **Testing = NUnit + Moq + FluentAssertions.** No xUnit anywhere. See testing standards.
- **Dev orchestration = Aspire; never deployed.** Prod = App Service + MSIX.
- **One MSIX** packages Kiosk.UI + Kiosk.Api + Devices + Diagnostics, auto-updated via `.appinstaller` from Azure Blob/CDN; `Kiosk.Api` runs as a logon-launched process (MSIX cannot host a Windows service). Staged rollout + health-gated rollback controlled from AdminPortal.
- **DB = PostgreSQL 18**, snake_case naming convention, `Npgsql.EnableLegacyTimestampBehavior` switch at the very top of every Program.cs, RLS per tenant (`app.tenant_id` GUC). Local dev DBs: `goldkiosk_local`, `goldkiosk_crm_local`.
- **Configuration**: per-machine `kiosk-settings.json` (ProgramData) + cloud layers — Key Vault for secrets, Blob for images/static JSON config with local cache. See configuration-and-operations standard.
- **Logging**: local-first. Everything to the machine's transaction log folder (Serilog rolling files); only Warning+/exceptions and whitelisted business telemetry go to Application Insights. See configuration-and-operations standard.
- **Monorepo, one solution**; `.slnf` filters (`KioskEdge.slnf`, `Cloud.slnf`) for focused loads.

## 4. Hard rules (non-negotiable)

1. **Branch first.** Never commit to the default branch. `feature/<ticket-id>-<slug>`.
2. **Never run `dotnet test`** — the user runs tests in Visual Studio. Verify with `dotnet build GoldKiosk.slnx -warnaserror` (must pass) and `dotnet format --verify-no-changes`.
3. **Secrets never touch files.** Dev: user-secrets + Aspire secret parameters. Prod: Key Vault via managed identity (cloud) / device identity (kiosk). Agents print user-secrets commands for the human to run; they never see or write secret values.
4. **Postgres conventions are sacred** (§3). Never altered as a side effect.
5. **Additive discipline.** No drive-by refactors, no restructuring outside ticket scope.
6. **Fixed dev launch ports** (see plan §Ports) so Postman/collections keep working.
7. Do not modify `.claude/settings.json`, CI workflows, or this file unless the task is about them.
8. Ask before destructive actions: dropping DB objects, deleting migrations, force-push, breaking a Contracts version.
9. PII/KYC (Aadhaar, PAN, photos, bank details) never in logs, never sent to third-party APIs including LLMs.

## 5. Definition of Done (every ticket)

- [ ] `dotnet build GoldKiosk.slnx -warnaserror` and `dotnet format --verify-no-changes` pass.
- [ ] NUnit tests written by **test-writer** for new/changed behavior (≥ 80% branch on new code; money/settlement paths 100%).
- [ ] Public APIs have XML docs; Contracts changes are additive and versioned.
- [ ] **code-reviewer** verdict is APPROVE / APPROVE-WITH-NITS; blockers resolved.
- [ ] Conventional commit (`feat:`/`fix:`/`test:`/`chore:`) referencing the ticket.
- [ ] Config keys added to the layering doc table; telemetry events added to the whitelist if cloud-bound.

## 6. Workflow & agents

1. **architect** — design note first for anything spanning >1 project, touching Contracts, data model, MSIX layout, or config layering. Binding once approved.
2. **developer** — implements per note + standards.
3. **test-writer** — NUnit coverage; never edits production code.
4. **code-reviewer** — read-only diff review; fixed verdict format.

Use `/implement-ticket <id>` to run the whole loop. Keep the main session as orchestrator;
delegate heavy exploration to subagents.
