# GoldKiosk Platform — Enterprise Build Kit

This folder is the **staging ground** for building the new GoldKiosk platform on .NET 10 with
.NET Aspire dev orchestration. It contains documentation, standards, Claude Code configuration,
and ready-to-paste prompts — not application code.

> The legacy `../goldkiosk-platform` repo is **reference material only** (business rules, flow
> coordinators, SQL, test scenarios). New code is authored fresh under the enterprise structure
> and standards defined here.

## The platform at a glance

| Tier | Projects | Ships as |
|---|---|---|
| Cloud | Cloud.Api, Cloud.AdminPortal, Cloud.CrmPortal | Azure App Service |
| Kiosk (edge) | Kiosk.UI (Blazor Hybrid), Kiosk.Api, Kiosk.Devices, Kiosk.Core, Kiosk.Diagnostics | **One signed MSIX** with `.appinstaller` auto-update |
| Shared | Domain, Application, Infrastructure, Contracts | libraries |
| Dev-only | AppHost, ServiceDefaults | never deployed |

Key operational decisions: per-machine `kiosk-settings.json` + Key Vault/Blob cloud config;
local-first logging to the machine transaction folder with only exceptions/whitelisted events
going to Application Insights; NUnit + Moq + FluentAssertions for all tests.

## Contents

| Path | What it is |
|---|---|
| `CLAUDE.md` | Constitution + locked decisions; auto-loads the standards. **Read first.** |
| `ASPIRE-TRANSFORMATION-PLAN.md` | Build plan T0–T14 with commands and code. |
| `KICKOFF-PROMPTS.md` | Prompts 1–9, one work chunk each. |
| `docs/standards/` | Binding standards: C#, architecture, testing (NUnit), security, configuration & operations, packaging & deployment. |
| `.claude/settings.json` | Permissions: allow / ask / deny (committed, team-shared). |
| `.claude/agents/` | architect, developer, test-writer, code-reviewer subagents. |
| `.claude/commands/` | `/implement-ticket`, `/review`. |

## How to pick it up

1. Copy this folder's contents to your repo root (or start sessions here).
2. `claude` → run `/agents` and confirm the four agents loaded.
3. Paste **Prompt 1** from `KICKOFF-PROMPTS.md`; proceed in order.
4. You run tests in Visual Studio between prompts — agents never run `dotnet test`.

## Permission model (summary)

- Allowed: build, restore, format, new/sln/add, git local operations, read-only EF commands.
- Ask: push, merge/rebase, `dotnet run`, `dotnet ef database update`, publish.
- Denied: `dotnet test`, force-push, hard reset, db drop, reading `.env`/secrets files,
  `dotnet user-secrets set` (agents print commands; you run them), editing settings.json/CI.

Personal overrides go in `.claude/settings.local.json` (git-ignored).
