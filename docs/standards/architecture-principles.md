# GoldKiosk Architecture Principles

## 1. Clean Architecture, pragmatic
Dependency rule: **hosts → Application → Domain**; Infrastructure implements Application
ports and is composed only in hosts.

- **GoldKiosk.Domain**: entities, aggregates, value objects (Money, Purity, GoldWeight), domain events, domain services. BCL only.
- **GoldKiosk.Application**: one use case per handler, validators, ports (`IGoldPriceProvider`, `ITransactionRepository`, `IKycService`, `IStaticConfigStore`, `IImageStore`), pipeline behaviors (validation/logging/transaction).
- **GoldKiosk.Infrastructure**: EF Core + Npgsql, Key Vault, Blob storage, GoldAPI/Claude API clients, App Insights plumbing. One adapter per port.
- **GoldKiosk.Contracts**: versioned wire DTOs + ProblemDetails type codes + SignalR contracts shared edge↔cloud. References nothing; additive changes only within a version.
- **Hosts** (Cloud.Api, Cloud.AdminPortal, Cloud.CrmPortal, Kiosk.Api, Kiosk.UI): composition roots, thin. A host file with an `if` on business state is a smell.
- **Kiosk.Core**: edge-local domain — durable transaction queue/outbox, price-staleness policy, session state machine. No cloud types.
- **Kiosk.Devices**: hardware ports + adapters (dispenser, scale, printer, camera, UPS) with **simulator implementations** selected by config so the full platform runs on dev laptops.

## 2. Vertical slices inside layers
Organize by feature: `Application/Transactions/CreateTransaction/{Command,Handler,Validator}.cs`.
A new feature = a new folder; minimal edits to shared files.

## 3. SOLID as applied here
- **S**: one use case per handler; one entity configuration per file; one hardware port per device class.
- **O**: new pricing rules/payout providers = new strategy registrations, not scattered switches.
- **L**: simulators honor the exact device port contracts (enforced by shared contract-test fixtures).
- **I**: narrow ports — `IGoldPriceProvider.GetSpotPriceAsync`, not `IExternalServices`.
- **D**: hosts depend on abstractions; Infrastructure and Devices are swappable.

Also binding: DRY with rule-of-three, YAGNI, KISS, composition over inheritance,
fail fast at boundaries / resilient across them.

## 4. Edge/cloud split (the defining constraint)
The kiosk must work when the internet doesn't.
- **Local durability first**: every customer transaction is persisted by Kiosk.Api locally (file-based transaction folders per ADR 0002 — no kiosk-local SQL) before any cloud call; a **queue-and-forward outbox** syncs to Cloud.Api.
- **Idempotency keys** on all edge→cloud writes; cloud endpoints are idempotent consumers; reconciliation compares edge outbox vs cloud ledger and alerts on discrepancy — never auto-fixes silently.
- **Gold price staleness policy** in Kiosk.Core: cached price with explicit TTL; stale price locks buy/sell with a clear customer-facing state — never a silent wrong price.
- **Hardware isolation**: only Kiosk.Api talks to Devices; Kiosk.UI consumes REST + SignalR. UI crash never loses a transaction; Kiosk.Api restart is safe mid-session (state machine resumes or safely aborts with audit).
- Contracts change additively; the fleet updates slowly (MSIX staged rollout).

## 5. Aspire (dev-only orchestration)
- AppHost models: connection-string resources (native Postgres), cloud-api, admin-portal, crm-portal, kiosk-api, kiosk-ui (desktop resource), secret parameters.
- Every **web** host uses ServiceDefaults: OpenTelemetry traces/metrics/logs, standard resilience handler on HttpClients, `/health` + `/alive`, service discovery. Desktop/windows projects never reference ServiceDefaults.
- Aspire is never deployed; nothing takes a runtime dependency on the AppHost. Prod = App Service + MSIX.

## 6. Data
- One PostgreSQL 18 cluster; separate databases: platform (`goldkiosk`) and CRM (`goldkiosk_crm`). No cross-database joins — integrate via Application layer or events.
- RLS per tenant via `app.tenant_id` GUC, set by a single EF interceptor.
- Aggregates are the consistency boundary; cross-aggregate workflows via domain events + outbox.
- Financial and KYC mutations write immutable audit records (actor, device, before/after, correlation id).
- Images and static JSON configuration live in **Blob storage** (see configuration-and-operations); Postgres stores references, never blobs.

## 7. Security architecture
- Dashboards: OIDC; kiosk machines: per-device identity (cert/credential) — device id flows into every audit record and telemetry item.
- Policy-based, resource-scoped authorization (kiosk operator sees only their kiosk; CRM roles ≠ Admin roles).
- PII/KYC encrypted at rest, masked in UI, access-audited, never logged, never sent to third-party APIs (including LLMs) without masking.

## 8. Decision records
Deviations require an ADR in `docs/adr/NNNN-title.md` (context, decision, consequences)
committed with the change. Standing ADR subjects already expected: edge local store choice,
outbox delivery semantics, MSIX watchdog strategy, Blazor Server vs MVC for portals.
