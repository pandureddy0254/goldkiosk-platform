# GoldKiosk Security Standards

Financial + KYC platform: every change is security-relevant.

## Secrets
- Dev: dotnet user-secrets + Aspire secret parameters. Cloud prod: **Azure Key Vault via managed identity**. Kiosk prod: secrets delivered through the config layer backed by Key Vault, authenticated by **device identity** — never baked into the MSIX or `kiosk-settings.json`.
- Never in: source, committed appsettings, launchSettings, logs, test fixtures, comments, sample files (use `<from-key-vault>` placeholders).
- Known secrets: `Claude__ApiKey`, `GoldApi__ApiKey`, JWT signing key, payment gateway creds, DB connection strings with credentials, MSIX signing cert/password (pipeline-only).
- Agents never see or write secret values: they print the `dotnet user-secrets` commands for the human and reference secrets by parameter name only. `dotnet user-secrets set` is deny-listed.

## Authentication & authorization
- Portals: OIDC; cookies `Secure`, `HttpOnly`, `SameSite=Lax` minimum.
- Kiosk machines: per-device certificate/credential; device identity attached to every request, audit record, and telemetry item. Compromised device = revoke identity centrally.
- Every new endpoint declares an authorization policy explicitly — `[AllowAnonymous]` requires an ADR. Resource-based checks for kiosk-scoped and customer-scoped data. RLS (`app.tenant_id`) enforced by the single EF interceptor — never bypassed.

## Input & output
- All external input validated at the boundary (FluentValidation / endpoint filters) before Application.
- EF parameterization only; no concatenated SQL.
- No `MarkupString`/`Html.Raw` with user data.
- KYC uploads: allow-listed content types, size limits, malware-scan hook, stored in the restricted `kyc-docs` Blob container with random names — never web-root, never public SAS.

## PII / KYC data
- Restricted class: Aadhaar, PAN, phone, photos, bank details.
- Encrypted at rest; masked in UI by default (last 4); access audited.
- Never in logs (local or App Insights), never in traces/metric tags, never sent to third-party APIs — LLM prompts must be scrubbed/masked before any Claude API call.
- Retention/deletion per policy; soft delete + audited purge job.

## Financial integrity
- Idempotency keys on all payment/dispense/settlement commands; outbox delivery at-least-once with idempotent cloud consumers.
- Immutable append-only audit log for money and KYC mutations: actor, device id, before/after, correlation id.
- Optimistic concurrency on financial aggregates; conflicts surface, never last-write-wins.
- Reconciliation (edge outbox vs cloud ledger) alerts on discrepancy; never auto-fixes silently.

## Edge machine hardening (with packaging standard)
- Kiosk runs under a standard (non-admin) account with Assigned Access; Kiosk.Api binds to localhost only.
- `%ProgramData%\GoldKiosk\` ACL'd to the kiosk service accounts; transaction store protected.
- Network egress allow-list; no inbound exposure.
- Update integrity: MSIX signature chain is the trust boundary; appinstaller URIs are HTTPS-only.

## Dependencies & supply chain
- Central package management, pinned versions; new package = justification + license check in PR; prefer first-party.
- `dotnet list package --vulnerable` clean before a release branch cut.

## Agent-specific guardrails
- Deny-listed (see `.claude/settings.json`): force-push, hard reset, db drop, `dotnet test`, reading `.env`/secrets/`*.pfx`/`*.snk`, `dotnet user-secrets set`, editing settings.json/CI workflows.
- Any change relaxing authorization, cryptography, audit behavior, telemetry whitelist, or the PII rules requires explicit human sign-off in the session before implementation.
