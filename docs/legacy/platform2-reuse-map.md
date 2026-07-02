# platform2 Reuse Map — GK-1

Date: 2026-07-02
Sources: six parallel assessments of `../goldkiosk-platform2` (src, tests, docs) against
this repo's binding standards. Companion to `legacy-parity-analysis.md` (the GoldCube-era
legacy) and `reference-docs-audit.md` (older architecture docs).

## Executive summary

platform2 is a **recent .NET 10 rebuild attempt**, not old legacy. Roughly **60–70% of its
logic is reusable**, but almost no file lands untouched because layering (Clean
Architecture), wire format, error model (ProblemDetails), auth (OIDC), and logging
(Serilog) all change per our standards. The DB schema + RLS design, the EF
interceptor pair, the Ed25519 licensing subsystem, the AI services, the hardware
abstraction + simulators, and the RLS-aware test harness are the crown jewels.

**Critical mismatch to know:** platform2's *docs* describe an ambitious per-machine kiosk
device API (session state machine, outbox, SignalR, device sidecars) that was **never
built** — the shipped `GoldKiosk.Api` is a small cloud/tenant data API. So for our
Kiosk.Api we inherit **blueprints** (high quality, incl. a 3,558-line OpenAPI yaml); for
Cloud.Api we inherit **working code**.

## Rotate-now security list (all committed in platform2 — treat as compromised)

| Secret | Location | Action |
|---|---|---|
| **Ed25519 license-signing PRIVATE key** | CRM.Web `appsettings.json` | Generate new keypair; rotate via existing `ActiveKid`/JWKS multi-key support. Root of the licensing trust chain. |
| AWS SES access key + secret | CRM.Web `appsettings.json` | Rotate in AWS IAM |
| GoldAPI.io API key (live) | Api `appsettings.Development.json` | Rotate at goldapi.io |
| Dev JWT signing key | Api `appsettings.Development.json` | Never reuse; prod key from secret store |
| DB passwords (dev) | CRM.Web `appsettings.Development.json`, test fixtures | Local-only; do not carry patterns forward |

(The GoldCube-era legacy secrets list is in `legacy-parity-analysis.md` §11.)

## Per-project verdicts

### GoldKiosk.Api (51 cs) → mostly GoldKiosk.Cloud.Api

| Component | Verdict | Effort |
|---|---|---|
| Claude AI vision item-validation + offer-explanation services (structured output, grounded prompts, degrade-gracefully) | Reuse (re-home to Application/Infrastructure) | S |
| GoldAPI price provider + 24h rate-sync hosted worker (multi-instance guard, backoff) | Reuse (re-home, add Serilog/OTel) | S–M |
| RegionOptions currency seam (India pivot) | Reuse as-is | S |
| Kiosk auth (Code+PIN → JWT via Argon2id) | Adapt (centralize key handling; evolve toward device identity per ADR 0003) | S–M |
| Offer pricing math | Adapt: keep formula, move to Domain, **externalize hardcoded 0.92 payout factor / 300s lock / fallback rates to tenant config** | M |
| Data services (languages/screensavers/terms/categories) | Adapt behind RLS interceptor (today: app-level `.Where(TenantId)` only — **no RLS**) | M |
| 8 MVC controllers | Rewrite → Minimal API endpoint classes + TypedResults + ProblemDetails + FluentValidation + versioned groups | M |
| DTOs | Adapt → GoldKiosk.Contracts; resolve camelCase-vs-snake_case and `PurityPct`/`PurityPercent` drift | M |
| Direct coupling to AdminDashboard.Data/.Identity | Re-architect behind Application ports | L |
| `goldkiosk-kiosk-api.yaml` + API-CONTRACT.md (44-command REST mapping, money envelope, RFC 7807 catalogue) | **Adopt as the Kiosk.Api contract-first baseline** in Contracts (normalize wire format, India-ize examples) | M |
| ADR-003 device hosting / ADR-005 outbox / ADR-006 persisted session / ADR-007 SignalR + DEVICE-ARCHITECTURE.md | Design input for building Kiosk.Api fresh (no code exists) | L (build) |

### GoldKiosk.KioskApp (WPF, 133 cs) → Kiosk.UI / Kiosk.Core / Kiosk.Devices

| Component | Verdict | Effort |
|---|---|---|
| `IHardwareDevice` + 7 role interfaces + `CameraRole` | Reuse as-is → Kiosk.Devices | S |
| `SimulatedDevice` base + 7 simulators + fault injection + latency multiplier + `HardwareManager` debounce | Adapt → Kiosk.Devices behind REST/SignalR | M |
| `HardwareOptions` per-device Real/Mock selection | Reuse as-is (already matches ADR 0004) | S |
| `TransactionFlowCoordinator` (15-state machine, recovery model, CTs everywhere) + `TransactionContext` | Adapt → Kiosk.Core; swap device calls for Kiosk.Api clients; add TimeProvider + durability | L |
| ViewModels + `KioskError`/`RecoveryAction`/`PayoutResult` models | Adapt → Kiosk.UI (strip hardcoded demo Aadhaar/PAN/mobile pre-fills) | M |
| XAML views + theme | Rewrite → Razor/CSS (design tokens transcribe) | L |
| HTTP clients (JWT cache, 401 retry) | Reuse | S |
| Sim KYC / payout / pricing services | Adapt → server-side behind real providers | M |
| Localization + voice | Rewrite (picker only today; zero translated strings, TTS is a log stub) | M–L |
| Durability / outbox / resume | **Build new** (nothing exists; a crash loses even a completed payout) | L |

New bounded problem from our UI/hardware split: **camera live-preview must become a
stream** (MJPEG/WebRTC/SignalR) instead of an in-process capture control — goes in the T4
design note.

### GoldKiosk.AdminDashboard + .Data + .Identity (269 cs) → Cloud.AdminPortal + shared layers

| Component | Verdict | Effort |
|---|---|---|
| SQL schema `db/admin-dashboard` (140 tables, FORCE RLS, triggers, DEFINER fns) | Reuse as-is per ADR 0005 (with its enumerated fixes) | M |
| `TenantContextInterceptor` (`app.tenant_id` GUC) + `AuditActorInterceptor` + marker interfaces | Reuse/adapt (verify GUC LOCAL-scope pooling semantics with an integration test; source tenant from OIDC claim) | S |
| `Argon2idPasswordHasher` | Reuse as-is | S |
| 56 POCO entities + 56 `IEntityTypeConfiguration` (zero annotations) | Reuse/adapt; **extend to the 7 unmodelled schemas** (payment, store, tenancy, doc, config, integration, compliance) | M |
| RBAC (roles/permissions/modules) + per-action `[Permission("area:action")]` (~120 attributes) | Adapt (rebase on OIDC principal; consider framework policies) | M |
| Activation-key PG functions + redemption | Adapt (also feeds per-device provisioning) | M |
| ~94 service files (fleet, operations×8, merchants, vouchers, customers, reports, monitoring, tenant settings, users) | Adapt → Application/Infrastructure; add Serilog (PII-scrubbed), `ValidateOnStart`, Contracts DTOs | L |
| Password/cookie sign-in | Rewrite → OIDC (`AppUser.ExternalSubject` already anticipates Entra) | M–L |
| Razor views | Rewrite (portal UI decision: Blazor Server vs MVC — open architect call) | L |
| Customer PII "encryption" (UTF-8 stub!) + wallet PIN (plain SHA-256) | **Rewrite before anything ships** | M |
| MFA (schema-only, OtpMode hardcoded "off") | Defer to IdP MFA or implement | M |

### GoldKiosk.CRM.Web (69 cs) → Cloud.CrmPortal + shared layers

| Component | Verdict | Effort |
|---|---|---|
| **Ed25519 `LicenseSigner` + payload + offline verifier + JWKS/revocation/verify API** | Reuse near-verbatim (keys → secret store) | S |
| Onboarding wizard (mark-won → partner/tenant → license → email) | Adapt → application service, **single transaction** (today a mid-sequence failure loses the recorded key), get tenant id before signing (kills the double-sign quirk) | M |
| DEFINER-RPC call pattern (parameterized `SqlQueryRaw`/`ExecuteSqlInterpolated`) + `UserContextInterceptor` | Adapt (GUC scope fix) | S–M |
| SES email, QuestPDF proposal generator | Reuse/adapt | S–M |
| Lead pipeline / partners / subscriptions read surfaces | Adapt | M |
| Cookie+bcrypt auth, custom role attributes | Rewrite → OIDC + policies | L |
| **Stripe ingestion + renewal cron** | **Build new — despite UI copy, no Stripe code or hosted service exists** | L / M |
| Stub controllers (Tasks/Inbox/Kiosks/ApiCredentials) | Drop | S |

### tests/ (16 projects, ~223 real tests) → tests/ + GoldKiosk.TestKit

- **Already NUnit 4.x + FluentAssertions 6.12.2 (free line)** — no framework conversion.
  Pin FluentAssertions **≤7** in Directory.Packages.props (8.x+ is commercially licensed).
- `Tests.Shared` → seed of **GoldKiosk.TestKit**: real-Postgres bootstrap, Respawn
  checkpoint over 18 schemas, tenant seeding, **RLS GUC per test**, `FakeCurrentUserService`.
  Port the pattern; replace the `psql` shell-out + hardcoded paths with Testcontainers.
- Port all feature suites (isolation/audit/permission specs are executable business
  rules); expect body churn where schema/service shapes move (Reports/Sales most).
- `CRM.Web.Tests` (empty xUnit+Playwright template) → delete.

## Cross-cutting rewrite themes (apply to everything on the way in)

1. Clean Architecture re-homing: hosts thin; logic → Application; EF/external → Infrastructure; DTOs → Contracts.
2. Serilog with PII-scrubbed templates (several email/PII log lines exist today).
3. RFC 7807 ProblemDetails with stable type codes (absent everywhere).
4. FluentValidation + options `ValidateDataAnnotations().ValidateOnStart()` (absent).
5. RLS interceptor everywhere Cloud-side; app-level tenant filters demoted to defense-in-depth.
6. OIDC for portal staff auth; JWT/device identity for kiosks (→ mTLS per sovereignty mandate).
7. Minimal API endpoint classes + TypedResults + versioned route groups for APIs.
8. TimeProvider instead of DateTime/DateTimeOffset.Now (few spots).
9. Business constants (payout factor, offer lock TTL) → tenant configuration.
10. snake_case wire format + `{amount_minor, currency, display}` money envelope decision enforced in Contracts.
