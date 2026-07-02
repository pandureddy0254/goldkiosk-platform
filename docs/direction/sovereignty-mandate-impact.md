# Platform Sovereignty Mandate — Impact on the Rebuild

Date: 2026-07-02
Sources: `CloudAnalysisDocs/Gold Kiosk Cloud Ownership One Pager.pdf`,
`Gold_Kiosk_Implementation_Guide.pdf` (CEO mandate, AI Kiosks International Inc.), and the
engineering `Platform-Sovereignty-Response/` package (dated 2026-06-15, **awaiting CEO
review**). This note records what is binding, what it adds to our plan, and what is open.

## The mandate in one paragraph

Tesla model: *"The kiosk is a terminal. The cloud is the product."* All production cloud
accounts, repos, PKI, OTA, AI hosting, and device certificates are owned by the US parent;
manufacturers/partners get scoped role-based IAM only — never admin credentials, never
their own subscriptions, no parallel/shadow clouds. Kiosks authenticate with per-device
certificates over **mTLS ("no auth, no operation")**; **pricing and transaction
authorization originate in the cloud**; OTA is signed and cloud-served; GoldKiosk retains
**9 remote control actions** (disable kiosk, suspend, maintenance mode, disable
transactions, disable payouts, restrict features, revoke certs, push updates, reassign
ownership).

## Locked by the CEO (binding on us)

- Ownership: single GoldKiosk-owned production tenant; manufacturer least-privilege IAM.
- Per-kiosk X.509 identity + mTLS before any operation (reinforces ADR 0003).
- Cloud-origin pricing and cloud transaction-authorization gate (kiosk never self-prices
  or self-authorizes — constrains our offline posture: stale-price lockout is the offline
  behavior, never local pricing).
- Signed OTA from GoldKiosk cloud (our MSIX + rings model fits).
- Retained-control plane with the 9 actions + kill-switch semantics.
- AI models/weights/inference stay under GoldKiosk control (our Claude API usage is
  cloud-side already; no on-kiosk models).
- Phasing: Phase 1 ownership/control → Phase 2 AI/OTA/control-plane → Phase 3 HSM/secure
  boot/attestation. **MVP gate for first 5 kiosks**: cloud ownership, repos, IAM,
  authentication, pricing service, authorization service, OTA, remote disable.
  HSM/attestation explicitly deferred (software keys in KMS/Key Vault for MVP).
- Cost envelope: ~$500/mo per 50 kiosks lean (validated by the response cost model);
  third-party per-transaction fees modeled separately.

## Explicitly OPEN (do not treat as decided)

| Decision | Status |
|---|---|
| **Cloud provider (Azure vs AWS)** | CEO table says **"TBD (Owned by Gold Kiosk)"**. Team leans Azure-primary/AWS-reversible; **awaiting CEO review since 2026-06-15**. Older workspace docs saying "Azure locked" are ahead of the decision. |
| CRM consolidation off AWS EB + Supabase + Stripe into the single tenant | Recommended Phase 1; not ratified |
| Dedicated HSM now vs Phase 3 | Leaning Phase 3 (~$2,300/mo driver) |
| Device CA build (EJBCA/step-ca) vs buy (AWS Private CA/Keyfactor) — note Azure has no managed private CA | Open |
| Offline grace window (how long a kiosk may operate cloud-disconnected; default fail-closed) | Open — directly shapes Kiosk.Core staleness policy |
| Attestation hardware (TPM/secure element on chosen units) | Open |

## What this adds to our plan (new scope)

1. **Device control plane** in Cloud.Api + AdminPortal: `device_commands` queue
   (push+poll+idempotent ACK, audited), entitlement state, **disable-on-non-payment**
   driven from CRM billing → entitlement events.
2. **Device PKI**: enrollment (CSR → GK CA), renewal, revocation (CRL/OCSP), per-kiosk
   cert with tenant+kiosk identity; mTLS termination in front of Cloud.Api.
3. **Cloud pricing/authorization gates**: signed price quotes; a transaction cannot
   settle without a cloud authorization record (ties into ADR 0002 outbox: offline =
   queued, never self-authorized settlement — reconcile in the T4/T5 design notes).
4. **8 control-plane tables** (spec'd in the response PRD, slot into ADR 0005 baseline):
   `device_certificates`, `certificate_revocations`, `firmware_releases`,
   `ota_deployments`, `device_commands`, `transaction_authorizations`,
   `device_telemetry`, `device_entitlements`.
5. **Manufacturer IAM tier** in RBAC (PR-only repo access, never prod admin).
6. Governance (business-side, tracked not built): work-for-hire/source-assignment
   agreements, source escrow, single-tenant consolidation before manufacturer builds.

## Confirmations (no change needed)

- **PostgreSQL** explicitly affirmed ("the build is PostgreSQL, not Azure SQL").
- MSIX + staged rings aligned with the mandate's signed-OTA requirement.
- Per-device identity (ADR 0003) is not just aligned — it's mandated.
- Provider-reversibility: our Clean Architecture Infrastructure seam is the mechanism;
  avoid provider-proprietary coupling outside Infrastructure adapters.

## Actions

- [ ] Chase CEO sign-off on the response package (provider + 5 open decisions above).
- [ ] T-plan additions: control-plane tables (db), device PKI/mTLS design note,
      command-queue + entitlement flows, cloud pricing/authorization gate in Cloud.Api.
- [ ] Keep all Azure-specific choices behind Infrastructure ports until the provider
      decision is ratified.
