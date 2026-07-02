# GoldKiosk Packaging & Deployment Standard

## 1. Shipping model

| Tier | Artifact | Mechanism |
|---|---|---|
| Cloud (Cloud.Api, AdminPortal, CrmPortal) | App Service deployments | CI → deployment slots, swap on health |
| Kiosk (edge) | **One signed MSIX** | `.appinstaller` auto-update from Azure Blob/CDN, staged rollout rings, health-gated rollback via AdminPortal |

Aspire is never part of deployment.

## 2. The kiosk MSIX (GoldKiosk.Kiosk.Package)

Windows Application Packaging project containing, in one package:
- **GoldKiosk.Kiosk.UI** — entry application (Blazor Hybrid shell). Declared as the app entry point.
- **GoldKiosk.Kiosk.Api** — full-trust process, **logon-launched by the UI shell** (MSIX cannot host Windows services). The shell owns lifecycle: start, health-ping, watchdog restart with backoff, clean shutdown.
- **GoldKiosk.Kiosk.Devices** — deployed as libraries with the API.
- **GoldKiosk.Kiosk.Diagnostics** — self-test exe, exposed as a secondary app/alias so field technicians can launch it from the machine.

Rules:
- Package identity, publisher, and capabilities live in the manifest; version is **stamped by CI** (never hand-edited); `x64` only unless an ADR says otherwise.
- Signing certificate comes from the pipeline secret store — the cert, its password, and any `.pfx` never enter the repo (deny-listed in `.claude/settings.json`).
- Local dev builds may be unsigned/self-signed for testing; anything leaving a dev machine is pipeline-signed.
- User data (config cache, logs, transaction store) lives in `%ProgramData%\GoldKiosk\`, **outside** the package, so updates never touch operational data.

## 3. Auto-update (.appinstaller)

- `deploy/appinstaller/GoldKiosk.Kiosk.appinstaller` template: MainPackage URI on Blob/CDN, `HoursBetweenUpdateChecks`, on-launch check, `ShowPrompt=false` for silent kiosk updates, critical-update flag path.
- **Rollout rings**: `ring-canary` (lab machines) → `ring-pilot` (selected stores) → `ring-fleet`. Each ring is a distinct appinstaller URI/path; AdminPortal assigns machines to rings and promotes releases.
- **Health gate**: after update, the kiosk runs a silent self-test subset and reports `UpdateApplied` + `SelfTestSummary`; a failing ring halts promotion and AdminPortal can point the ring back at the previous version (rollback = re-publish prior package to the ring URI).
- Update windows respect trading hours: the shell defers applying updates during an active customer session.

## 4. Machine provisioning (deploy/setup-docs)

`provisioning-runbook.md` (kept current every release; generated sections where possible):
1. Windows baseline: edition, updates policy, power settings (never sleep), time sync.
2. Kiosk account: local standard user, **autologon + Assigned Access / shell replacement** to GoldKiosk.Kiosk.UI.
3. Trust: install the App Installer trust / cert chain; enroll device identity (cert) for cloud auth.
4. Author `%ProgramData%\GoldKiosk\config\kiosk-settings.json` from the schema + key table (KioskId, TenantId, StoreCode, hardware ports, transaction folder).
5. Network allow-list: Blob/CDN, Cloud.Api, App Insights, time servers, gold-price provider.
6. Install via appinstaller URI for the machine's ring.
7. Run **GoldKiosk.Kiosk.Diagnostics** full self-test; attach the signed report to the provisioning ticket. A machine ships only with a passing report.

## 5. Diagnostics exe (field/test software)

`GoldKiosk.Kiosk.Diagnostics` responsibilities:
- Hardware self-test through the same Devices ports as production (dispenser, scale, printer, camera, UPS), plus network reachability (Cloud.Api, Blob, App Insights), clock drift, disk space, config validation, cert expiry.
- Modes: full interactive (field tech), silent subset (post-update health gate), single-device probe.
- Output: human-readable + JSON report to `%ProgramData%\GoldKiosk\logs\diagnostics\`, one whitelisted `SelfTestSummary` telemetry event. Exit codes contract: 0 pass / 1 warnings / 2 failures (used by the health gate).

## 6. CI stages (definition; wiring is a later ticket)

build → `dotnet format --verify-no-changes` → NUnit test run (CI executes tests; Claude never does) → publish cloud apps → pack + sign MSIX → publish appinstaller to ring path → smoke. Release notes generated from conventional commits.
