# ADR 0003 — Kiosk connectivity: admin-platform backend only, per-kiosk identity

Date: 2026-07-02
Status: Accepted (owner decision, 2026-07-02)

## Context

The owner has decided kiosks talk to **the admin-portal backend only**. In the legacy
system every kiosk authenticated to a single PHP endpoint with one shared static key
(`md5('theGoldCube')`) — no per-device identity, no rotation, no revocation. The CRM is a
separate concern (partner/licensing/billing) with its own database.

## Decision

- The kiosk edge (`GoldKiosk.Kiosk.Api`) communicates **exclusively with
  `GoldKiosk.Cloud.Api`** — the shared backend of the admin platform, backed by the
  `goldkiosk` database. Interpreting the owner's "admin portal only": Cloud.Api and
  AdminPortal are two hosts over the same admin-platform backend/database; kiosks hit the
  API host, never the portal UI and never anything CRM-side.
- Kiosks have **no access path to the CRM database or CrmPortal**. CRM↔platform
  integration happens cloud-side only (activation-key handshake + `admin_tenant_id`
  pointer per the platform2 schema design).
- Every kiosk authenticates with a **per-device identity** (certificate/credential per
  the security standard); device id flows into every request, audit record, and telemetry
  item; compromise = central revocation. The legacy shared key is retired and treated as
  exposed.
- All kiosk-facing endpoints live under versioned routes on Cloud.Api and are consumed
  through the outbox/idempotency discipline of ADR 0002.

## Consequences

- One ingress surface to secure, rate-limit, and observe for the whole fleet.
- AdminPortal features (fleet, config push, rings, monitoring) operate on the same
  database the kiosks write through Cloud.Api — no sync between "admin" and "kiosk" data.
- The CRM remains independently deployable and its data residency is unaffected by kiosk
  traffic.
- Cloud.Api capacity/availability is the fleet's lifeline; kiosks must degrade gracefully
  offline (stale-price lockout, outbox queueing) per architecture principles §4.
