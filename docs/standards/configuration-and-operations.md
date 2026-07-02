# GoldKiosk Configuration & Operations Standard

Covers configuration layering (machine + cloud), secrets, static assets, logging, and
telemetry. Binding for all projects.

## 1. Configuration layering — kiosk (edge)

Precedence, lowest → highest (later wins):

| # | Layer | Source | Examples | Refresh |
|---|---|---|---|---|
| 1 | Package defaults | `appsettings.json` inside the MSIX | timeouts, retry counts, UI defaults | with app update |
| 2 | **Machine settings** | `%ProgramData%\GoldKiosk\config\kiosk-settings.json` | KioskId, TenantId, StoreCode, hardware COM ports/models, Kiosk.Api port, transaction folder path, locale | provisioning / field tech |
| 3 | **Cloud static config** | Blob storage JSON (per-tenant + per-kiosk overlay), cached locally with ETag | pricing display rules, screen flows, promo banners, feature flags, price-staleness TTL | poll interval + on-demand push |
| 4 | **Cloud secrets** | Key Vault (surfaced through Cloud.Api config endpoint; device identity auth) | GoldAPI key, payment gateway creds, SignalR/cloud tokens | on rotation |
| 5 | Environment variables | Aspire (dev only) | connection overrides, dashboards | dev |

Rules:
- `kiosk-settings.json` is authored per machine during provisioning (schema + example in `deploy/setup-docs`); it contains **identity and hardware facts only — never secrets**.
- Cloud static config is **cached** under `%ProgramData%\GoldKiosk\cache\config\`; the kiosk must boot and trade (within staleness policy) with cloud unreachable, using the last good cache. Cache misses on first boot = provisioning error surfaced by Diagnostics.
- Every section binds to an Options class with `ValidateDataAnnotations().ValidateOnStart()`; a kiosk with invalid config fails fast at startup with a Diagnostics-readable report, it does not limp.
- Config keys are documented in the table in `deploy/setup-docs/provisioning-runbook.md`; adding a key without updating the table is a review blocker.

## 2. Configuration — cloud apps

- Standard `appsettings.json` / `appsettings.{Environment}.json` for non-secrets; **Azure Key Vault configuration provider via managed identity** for secrets in Azure; user-secrets + Aspire parameters in dev.
- No secrets in any committed file, ever, including samples — sample files use `<from-key-vault>` placeholders.

## 3. Static assets — Blob storage

- Containers: `kiosk-config/` (static JSON, versioned paths `v{n}/tenant/{tenantId}/...`), `kiosk-assets/` (images: banners, product art, UI media), `kyc-docs/` (restricted, separate access policy, never public).
- Edge access is **read-only via SAS/device identity**; kiosks cache assets locally and verify by ETag/content hash. Cloud.Api owns writes.
- Publishing new config/assets is an AdminPortal action with audit; kiosks pick up on next poll or push notification (SignalR).

## 4. Logging — local-first

**Principle: the machine's transaction folder is the system of record for operational detail;
the cloud only receives what is actionable.**

Kiosk (Serilog):
- Rolling files under `%ProgramData%\GoldKiosk\logs\` :
  - `transactions\yyyy-MM-dd\` — per-transaction structured log (JSON lines): every step, hardware events, amounts, correlation ids. Retention per policy (default 180 days), size-capped.
  - `app\` — general application log, daily rolling, 30 days.
  - `diagnostics\` — self-test reports.
- Log context always includes: KioskId, TenantId, SessionId, TransactionId, CorrelationId.
- Local logs may contain full operational detail but still **never PII beyond what the transaction legally requires, and never secrets**.

Cloud sink (Application Insights) — **gated**:
- Severity gate: Warning and above (exceptions always).
- Plus an explicit **TelemetryEvents whitelist** (business signals worth fleet-level visibility):
  `TransactionCompleted` (ids + amounts only), `TransactionFailed`, `HardwareFault`,
  `PriceStalenessLockout`, `OutboxBacklogHigh`, `UpdateApplied`, `SelfTestSummary`, `ConfigApplied`.
  Adding an event = PR updating this list + review.
- Sampling on for traces/dependencies; custom events not sampled.
- Connection string for App Insights arrives via config layer 4, not baked into the package.

Cloud apps: Serilog → App Insights + OTel via ServiceDefaults (dev traces go to the Aspire
dashboard). Same PII rules.

## 5. Correlation & audit

- W3C traceparent propagates Kiosk.UI → Kiosk.Api → Cloud.Api; the same CorrelationId appears in local transaction logs, App Insights, and audit records.
- Financial/KYC mutations write immutable audit rows regardless of logging config — logging is observability, audit is a business record; they are never merged.

## 6. Operational jobs

- Log shipping is **not** default: local logs stay local; AdminPortal can request an on-demand log bundle upload from a kiosk (zipped, redacted) for support cases.
- Outbox monitor emits `OutboxBacklogHigh` when sync lag exceeds threshold.
- Disk-space guard: transaction logging must never fill the disk — size caps + oldest-first pruning with an audit event when pruning occurs.
