# ADR 0004 — Per-device mock/real selection via layered config (cloud overrides local)

Date: 2026-07-02
Status: Accepted (owner decision, 2026-07-02)

## Context

The owner requires: (a) every hardware device mockable so a full transaction can be
navigated end-to-end without physical hardware; (b) mock vs real chosen **per device** in
the settings file; (c) a cloud-delivered setting overrides the local value. The legacy
system already proves the pattern: `UseMockDevices` plus per-device overrides
(`UseMockCashDispenser`, `UseMockIdScanner`, `UseMockServerApi`, `UseMockMQ`) swap DI
registrations in `WindsorRegistrar.cs`. What legacy lacked: layering, validation, and
simulators good enough to complete a transaction.

## Decision

- Every device behind a `GoldKiosk.Kiosk.Devices` port ships **two first-class
  implementations**: the real driver and a **simulator** that honors the exact port
  contract (enforced by shared contract-test fixtures per the testing standard) and can
  drive a full happy-path transaction plus scripted failure modes.
- Selection is per device via Options config, e.g.
  `Devices:Scale:Mode = Real | Mock`, `Devices:MetalAnalyser:Mode`, … — bound with
  `ValidateDataAnnotations().ValidateOnStart()`.
- The value resolves through the standard config layering (configuration-and-operations
  §1): package defaults → **machine `kiosk-settings.json`** → **cloud static config
  (per-tenant + per-kiosk overlay, cached with ETag)** → env (dev). Later wins, so **a
  cloud-set mode overrides the local file** — exactly the owner's requirement — and the
  kiosk still boots from cache when the cloud is unreachable.
- Composition happens once at startup in Kiosk.Api's DI (switching modes = restart);
  the effective mode per device is logged at startup and reported in Diagnostics
  self-tests and the `ConfigApplied` telemetry event.
- Dev default (Aspire): all devices Mock, so the full platform runs on a laptop.
  Production provisioning default: all devices Real; a machine with any Mock device
  reports it loudly in health/telemetry (a kiosk must never silently trade on mocks —
  transactions executed with any mocked device are marked as test transactions in the
  transaction record).

## Consequences

- End-to-end mock transactions become a CI-able scenario and a field-diagnostic tool.
- The AdminPortal can flip a device to Mock remotely for triage (with the audit trail
  the config-publish action already carries).
- Simulators are production code with contract tests — not throwaway fakes.
- One new invariant to enforce in review: no code path may branch on "is mock" outside
  the composition root and the test-transaction marking.
