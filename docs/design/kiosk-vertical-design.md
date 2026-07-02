# Kiosk Vertical — Design Note (GK-2)

Date: 2026-07-02 · Status: Binding (owner-directed, autonomy granted)
Inputs: `D:\Claude\goldkiosk\goldkiosk-ui-design\DESIGN-DIRECTIVE-2026.md` + the six
**Obsidian** exports (`exports/obsidian-*.png`) as the visual reference; legacy parity
analysis; platform2 reuse map; ADRs 0002–0005.

## 1. Goals

- Blazor **Hybrid** kiosk UI (BlazorWebView in the WPF shell) in the Obsidian design
  language; rich cinematic UX per the directive (no gold, internals hidden, one idea per
  screen, no spinners).
- **Mock-first**: full transaction navigable end-to-end with simulated devices; real
  driver code structure in place for the existing machine's hardware.
- **Ships to the existing machine**: device ports cover the exact legacy hardware set.
- **Batched payloads**: the UI never posts per selection — selections accumulate and ship
  with the tray commands (and equivalents on the return path).
- Per-device bypass via settings (ADR 0004), cloud-overridable.

## 2. Screen map (Obsidian; consolidated from legacy's 19 screens)

| # | Screen | Obsidian ref | Consolidates (legacy) | Notes |
|---|---|---|---|---|
| 0 | Attract loop | `obsidian-intro.png` | Home/attract | Rotating "We buy Gold/Silver · Pawn" serif hero, tray medallion, TOUCH TO BEGIN. No logo/marketing. |
| 1 | Welcome / Service | **new** | FAQ + Start (sell/pawn + metal choice) | One screen: Sell or Pawn as two large glass cards. **No metal/karat question** — AI auto-detects. Language switch chip. |
| 2 | Place item | **new** | T&C + Description(type/size/style) + OpenTray + CloseTray | Tray opens on entry; guidance animation; terms acceptance inline (single checkbox + link sheet). Item description questions **removed** (AI detects); optional hint chip only. Confirm-close button. |
| 3 | Analysis | `obsidian-analysis.png` | Analysis + weight + LA item check | Orbital scan + 3-step checklist (detected/authenticity/pricing). No weight/karat/% shown. Live-agent escalation overlays here. |
| 4 | Offer | `obsidian-offer.png` | Offer | One big number, Verified/Live price/lock-countdown chips, "How was this calculated?" AI explainer sheet, Accept / Return item. Pawn variant shows repayment terms in the explainer sheet, not on the main screen. |
| 5 | Identity | `obsidian-identity.png` | ID scan + selfie + fingerprint + signature + contract | One checklist screen driving sequential hardware steps: ID scanned → Face match → (Fingerprint, config-gated) → Signature (on-screen draw + terms version). |
| 6 | Contact & receipt | **new** | Email + Phone screens | One screen: email + phone + receipt channels (QR always; email/SMS optional). One API call. |
| 7 | Payout | `obsidian-payout.png` | GetCashRefund + Dispense choice | Instant cash / Bank transfer / Debit card selection cards. Bank fields inline expand (single screen). Config-gates: cash requires dispenser healthy + bill mix. |
| 8 | Processing | **new** | Dispense/bagging waits | Robotic-motion visual (arm/bagging/dispense progress via SignalR). Never a spinner. |
| 9 | Done | `obsidian-done.png` | ThankYou | Check disc, QR receipt, auto-return to attract. |
| — | Overlays | **new** | TimerPanel, error popups, LA video | Idle-timeout countdown ring; error/recovery sheet (localized catalogue, recovery routing); live-agent video panel; operator/diagnostics gesture. |

Rejection paths reuse the Analysis→(rejected reason)→Return-item overlay→Done flow.

## 3. Session state machine (Kiosk.Core)

`attract → welcome → placing_item(tray_opening→awaiting_item→tray_closing) → analyzing →
offer → identity → contact → payout → settling(bagging→dispensing|transferring) → done`

Branches: `analyzing→rejected→returning_item→done`; `offer declined→returning_item`;
`abort(timeout|cancel|fault)→returning_item?→done`. Every transition persisted to the
transaction folder journal (ADR 0002) so Kiosk.Api restart resumes or safe-aborts with
audit. Offer carries a server-side lock TTL (chip countdown on screen).

## 4. Kiosk.Api surface (localhost, fixed port 7201)

Wire format: **snake_case JSON**; money envelope `{"amount_minor","currency","display"}`;
RFC 7807 errors with stable `type` codes; `Idempotency-Key` header mandatory on
state-changing session calls. SignalR hub `/hubs/kiosk` (server→client only; REST is the
source of truth; events carry a per-session monotonic `sequence`).

### REST (all under `/api/v1`)

| Endpoint | Purpose / batching rule |
|---|---|
| `POST /sessions` | Begin session at welcome tap. Returns session_id, feature flags, payout methods, offer TTL. |
| `GET /sessions/{id}` | Full state snapshot — UI crash/reload resume. |
| `POST /sessions/{id}/tray/open` | **Clubbed payload #1**: tray-open command + everything selected so far (service_type, locale, terms acceptance, optional item hint, attract analytics). No prior per-selection calls. |
| `POST /sessions/{id}/tray/close` | **Clubbed payload #2 (vice-versa)**: close command + has_item flag + batched client telemetry events. Triggers analysis. `has_item:false` = close-without-item. |
| `POST /sessions/{id}/offer/accept` · `/offer/decline` | Accept → identity; decline → return item. |
| `POST /sessions/{id}/offer/explain` | AI explainer ("How was this calculated?") — question optional. |
| `POST /sessions/{id}/identity/start` | Kicks the hardware-driven sequence (ID scan → face match → fingerprint if enabled). Progress via SignalR. |
| `POST /sessions/{id}/identity/signature` | Signature PNG + accepted terms version (clubbed). |
| `POST /sessions/{id}/contact` | **Clubbed**: email + phone + receipt channels in one call. |
| `POST /sessions/{id}/payout` | **Clubbed**: method + bank details (if transfer) in one call. |
| `POST /sessions/{id}/settle` | Bag + dispense/transfer. Terminal receipt in `session_completed` event + snapshot. |
| `POST /sessions/{id}/abort` | reason: timeout / user_cancel / operator / fault; returns item when held. |
| `POST /sessions/{id}/agent/item-check` | Live-agent escalation (shape informed by legacy LIVE_AGENT_* mining). |
| `GET /devices` · `POST /devices/{key}/probe` | Health snapshot; single-device diagnostic probe. |
| `GET /rates` | Attract-loop market ticker (display-safe). |
| `GET /health` · `/alive` | Standard. |

### SignalR events

`session_state_changed`, `tray_state_changed`, `analysis_progress`,
`identity_progress`, `settlement_progress`, `agent_status`, `device_health_changed`,
`session_completed`, `session_aborted`, `idle_warning`.

Full request/response examples: `docs/api/kiosk-api-payload-samples.md`.
Postman: `docs/api/GoldKiosk-Kiosk-Api.postman_collection.json`.

## 5. Devices (existing-machine parity) + bypass config

Ports in `GoldKiosk.Kiosk.Devices` (one interface per device, simulator + real for each):
`IScale` (COM5 MT-SICS), `IMetalAnalyser` (Vanta WebSocket + InnovX COM behind one port),
`IRoboticArm` (Dobot TCP/serial + AKD axis), `IVolumeChamber` (pressure COM4 + stepper
COM3), `ISensorBoard`/`IPowerRelays` (Advantech USB-4761), `ICameraService` (role-keyed:
customer/tray/item/live-agent), `IIdScanner` (Acuant | Gemalto), `IFingerprintScanner`
(FlexCode), `ICashDispenser` (Fujitsu F53/Envoy), `ILabelPrinter` (Brother b-PAC),
`IBagger`, UPS via sensor bit. platform2's `IHardwareDevice` base + simulator/fault-
injection pattern is the seed (reuse map §KioskApp).

Bypass (legacy `GoldCubeAppSettings.xml` capability, modernized per ADR 0004):

```jsonc
// kiosk-settings.json / appsettings — layered, cloud layer overrides local
"Devices": {
  "DefaultMode": "Real",              // fleet default
  "Overrides": {
    "Scale":        { "Mode": "Mock" },   // per-device bypass, like UseMockDevices/
    "CashDispenser":{ "Mode": "Mock" }    // UseMockCashDispenser in legacy XML
  },
  "Simulation": { "LatencyMultiplier": 1.0, "FaultDevices": [] }
}
```

Mocked responses are scriptable (happy path + failure modes) so any device can be
bypassed while the rest run real — matching the legacy field practice. Any session run
with ≥1 mocked device is flagged `is_test` in the transaction record (ADR 0004).

## 6. Mock-first build order

1. Contracts (DTOs + SignalR events + ProblemDetails types) — from this note.
2. Kiosk.Api endpoints + session state machine + file-journal durability (ADR 0002) with
   **all devices simulated** — full flow green end-to-end.
3. Kiosk.UI Blazor Hybrid — Obsidian screens against the live mock Kiosk.Api.
4. Real driver skeletons in Kiosk.Devices (ports compiled, SDK wiring staged per the
   legacy port map) — real mode selectable per device via config from day one.
5. Cloud.Api integration (offer/AI/KYC endpoints) — mockable via `UseMockServerApi`
   equivalent (`Cloud:Mode`).

## 7. Open items folded in when ready

- Live-agent item-check payloads (mining agent in flight) → agent endpoint + overlay.
- Cloud pricing/authorization gate placement (sovereignty mandate) → settle requires a
  cloud authorization record when online; offline behavior follows the CEO's
  grace-window decision (currently fail-closed for settlement, ADR 0002 outbox for data).
