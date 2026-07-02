# Kiosk API — Payload Samples (GK-2)

Companion to `docs/design/kiosk-vertical-design.md`. Wire rules: snake_case JSON; money
envelope `{amount_minor, currency, display}`; `Idempotency-Key` header on all
state-changing session calls; errors are RFC 7807 with stable `type` codes; SignalR
events carry a per-session monotonic `sequence`. Base URL (dev): `https://localhost:7201/api/v1`.

---

## 1. Begin session — `POST /sessions`

Request:
```json
{
  "locale": "en-US",
  "attract_source": "touch",
  "kiosk_time": "2026-07-02T14:03:11.412-05:00"
}
```

Response `201`:
```json
{
  "session_id": "ses_01JZC7H2Q4R8TN5M9W3VXK6P0D",
  "state": "welcome",
  "sequence": 1,
  "features": {
    "pawn_enabled": true,
    "crypto_enabled": false,
    "fingerprint_required": false,
    "payout_methods": ["cash", "bank_transfer", "debit_card"]
  },
  "offer_ttl_seconds": 600,
  "idle_timeout_seconds": 90,
  "terms_version": "2026-06-01.v3"
}
```

## 2. Tray open — `POST /sessions/{id}/tray/open`  ← clubbed payload #1

Everything selected before the tray goes up ships here — **no per-selection calls**.

Request (`Idempotency-Key: 7d3f0a1c-...`):
```json
{
  "command": "tray_open",
  "setup": {
    "service_type": "sell",
    "locale": "en-US",
    "terms": { "version": "2026-06-01.v3", "accepted": true, "accepted_at": "2026-07-02T14:03:58.101-05:00" },
    "item_hint": null,
    "promo_code": null
  },
  "client_events": [
    { "at": "2026-07-02T14:03:12.000-05:00", "event": "attract_engaged", "data": { "loop_frame": "we_buy_gold" } },
    { "at": "2026-07-02T14:03:41.220-05:00", "event": "service_selected", "data": { "service_type": "sell", "dwell_ms": 4180 } }
  ]
}
```

Response `202`:
```json
{
  "session_id": "ses_01JZC7H2Q4R8TN5M9W3VXK6P0D",
  "state": "placing_item",
  "tray": { "status": "opening" },
  "sequence": 4
}
```

## 3. Tray close — `POST /sessions/{id}/tray/close`  ← clubbed payload #2 (vice-versa)

Request:
```json
{
  "command": "tray_close",
  "has_item": true,
  "client_events": [
    { "at": "2026-07-02T14:04:31.876-05:00", "event": "item_placed_confirmed", "data": { "retries": 0 } }
  ]
}
```

Response `202`: `{ "state": "analyzing", "tray": { "status": "closing" }, "sequence": 7 }`

Close **without** item (customer backs out): `{ "command": "tray_close", "has_item": false }`
→ `{ "state": "welcome", "tray": { "status": "closing" } }`

## 4. Analysis progress — SignalR `analysis_progress`

```json
{ "session_id": "ses_01JZC…", "sequence": 9,  "stage": "item_detected",   "progress": 34, "display": "Item detected & classified" }
```
```json
{ "session_id": "ses_01JZC…", "sequence": 11, "stage": "authenticating",  "progress": 68, "display": "Verifying authenticity" }
```
```json
{ "session_id": "ses_01JZC…", "sequence": 13, "stage": "pricing",         "progress": 91, "display": "Pricing against live market" }
```

Terminal — SignalR `session_state_changed` (offer made; **no internals exposed**):
```json
{
  "session_id": "ses_01JZC…",
  "sequence": 14,
  "state": "offer",
  "offer": {
    "offer_id": "off_01JZC7NQ2B8XW4E5T6Y7U8I9O0",
    "amount": { "amount_minor": 846000, "currency": "USD", "display": "$8,460.00" },
    "kind": "sale",
    "verified": true,
    "live_price": true,
    "expires_at": "2026-07-02T14:15:12.000-05:00"
  }
}
```

Pawn variant adds:
```json
{
  "kind": "pawn",
  "pawn_terms": {
    "monthly_fee": { "amount_minor": 42300, "currency": "USD", "display": "$423.00" },
    "apr_percent": 60.0,
    "total_repayment": { "amount_minor": 930600, "currency": "USD", "display": "$9,306.00" },
    "due_date": "2026-08-01"
  }
}
```

Rejection terminal:
```json
{
  "state": "returning_item",
  "rejection": {
    "reason_code": "item.multiple_items",
    "display": "Please place one item at a time",
    "recovery": "retry_place_item"
  }
}
```
Reason codes (from legacy catalogue): `item.empty_tray`, `item.multiple_items`,
`item.unidentified`, `item.unaccepted_type`, `item.underweight`, `item.gold_plated`,
`item.insufficient_purity`, `kyc.underage`, `kyc.id_expired`, `kyc.not_govt_id`,
`kyc.blacklisted`, `kyc.face_mismatch`, `payout.insufficient_cash`.

## 5. Offer actions

`POST /sessions/{id}/offer/accept` → `202 { "state": "identity", "sequence": 16 }`
`POST /sessions/{id}/offer/decline` → `202 { "state": "returning_item", "sequence": 16 }`

`POST /sessions/{id}/offer/explain`:
```json
{ "question": "Why is my offer lower than the gold price I saw online?" }
```
```json
{
  "explanation": "Your offer reflects today's live market price for your item's verified precious-metal content, minus our margin. Market spot prices quote pure refined metal; jewellery contains alloys and the offer accounts for refining.",
  "disclaimer": "Offers are valid while the timer is running and may change with the live market."
}
```

## 6. Identity — `POST /sessions/{id}/identity/start`

Request: `{}` → `202 { "state": "identity", "identity": { "current_step": "id_scan" } }`

SignalR `identity_progress`:
```json
{ "sequence": 18, "step": "id_scan",    "status": "completed" }
```
```json
{ "sequence": 19, "step": "face_match", "status": "in_progress" }
```
```json
{ "sequence": 21, "step": "face_match", "status": "failed", "failure": { "reason_code": "kyc.face_mismatch", "retries_left": 2, "recovery": "retry_face_match" } }
```
Steps: `id_scan` → `face_match` → `fingerprint` (only when `features.fingerprint_required`) → `signature`.

Signature + terms (clubbed) — `POST /sessions/{id}/identity/signature`:
```json
{
  "signature_png_base64": "iVBORw0KGgoAAAANSUhEUgAA…",
  "signed_terms_version": "2026-06-01.v3"
}
```
→ `202 { "state": "contact" }`

## 7. Contact & receipt (one combined call) — `POST /sessions/{id}/contact`

```json
{
  "email": "customer@example.com",
  "phone": "+13125550147",
  "receipt_channels": ["qr", "email"]
}
```
→ `202 { "state": "payout" }`. Email/phone optional when `receipt_channels` = `["qr"]`.

## 8. Payout (one combined call) — `POST /sessions/{id}/payout`

Cash:
```json
{ "method": "cash" }
```
Bank transfer (fields inline-expanded on the same screen):
```json
{
  "method": "bank_transfer",
  "bank": {
    "account_holder": "Jordan Avery",
    "routing_number": "071000013",
    "account_number": "000123456789",
    "account_type": "checking"
  }
}
```
→ `202 { "state": "payout_confirmed", "payout": { "method": "cash", "bill_mix_ok": true } }`

Insufficient cassette cash → `409` ProblemDetails:
```json
{
  "type": "https://goldkiosk.dev/problems/payout.insufficient_cash",
  "title": "Cash unavailable for this amount",
  "status": 409,
  "detail": "Choose bank transfer or debit card, or return your item.",
  "session_id": "ses_01JZC…",
  "available_methods": ["bank_transfer", "debit_card"]
}
```

## 9. Settle — `POST /sessions/{id}/settle`

Request (`Idempotency-Key` mandatory): `{}` → `202 { "state": "settling" }`

SignalR `settlement_progress`:
```json
{ "sequence": 25, "stage": "bagging",    "display": "Securing your item" }
```
```json
{ "sequence": 27, "stage": "dispensing", "display": "Dispensing your cash", "bills": { "100": 84, "20": 3 } }
```

SignalR `session_completed`:
```json
{
  "sequence": 30,
  "state": "done",
  "receipt": {
    "receipt_id": "rcp_01JZC7ZM4K2Q…",
    "invoice_number": "USGK-000412",
    "qr_payload": "https://r.goldkiosk.com/t/rcp_01JZC7ZM4K2Q",
    "channels_sent": ["qr", "email"]
  },
  "is_test": false
}
```

## 10. Abort — `POST /sessions/{id}/abort`

```json
{ "reason": "timeout", "return_item": true }
```
Reasons: `timeout` · `user_cancel` · `operator` · `fault`.
→ `202 { "state": "returning_item" }` then SignalR `session_aborted`.

## 11. Live-agent item check — `POST /sessions/{id}/agent/item-check`

```json
{ "trigger": "analysis_escalation" }
```
SignalR `agent_status`:
```json
{ "sequence": 12, "status": "connecting" }
```
```json
{ "sequence": 15, "status": "approved", "agent_ref": "agt_204" }
```
Statuses: `connecting` · `agent_joined` · `approved` · `declined` · `unavailable`
(unavailable ⇒ policy fallback: AI verdict stands or session aborts — **never a fabricated
approval**, unlike legacy).

## 12. Devices

`GET /devices`:
```json
{
  "overall": "degraded",
  "devices": [
    { "key": "scale",          "mode": "real", "state": "ready",   "detail": null },
    { "key": "metal_analyser", "mode": "real", "state": "ready",   "detail": "vanta" },
    { "key": "cash_dispenser", "mode": "mock", "state": "ready",   "detail": "simulated" },
    { "key": "id_scanner",     "mode": "real", "state": "faulted", "detail": "device_not_found" }
  ]
}
```
`POST /devices/scale/probe` → `{ "key": "scale", "result": "pass", "elapsed_ms": 812 }`

## 13. Rates ticker — `GET /rates`

```json
{
  "as_of": "2026-07-02T14:00:00Z",
  "rates": [
    { "metal": "gold",   "per_gram": { "amount_minor": 10894, "currency": "USD", "display": "$108.94" }, "change_percent": 0.42 },
    { "metal": "silver", "per_gram": { "amount_minor": 132,  "currency": "USD", "display": "$1.32" },  "change_percent": -0.11 }
  ]
}
```

## 14. Snapshot (crash resume) — `GET /sessions/{id}`

```json
{
  "session_id": "ses_01JZC…",
  "state": "offer",
  "sequence": 14,
  "offer": { "offer_id": "off_01JZC…", "amount": { "amount_minor": 846000, "currency": "USD", "display": "$8,460.00" }, "expires_at": "2026-07-02T14:15:12.000-05:00" },
  "identity": null,
  "payout": null,
  "is_test": false
}
```

## 15. ProblemDetails type-code registry (initial)

```
session.not_found · session.invalid_state · session.expired
tray.blocked · tray.hardware_fault
offer.expired · offer.already_actioned
identity.step_failed · identity.retries_exhausted
payout.insufficient_cash · payout.invalid_bank_details
settle.authorization_required · settle.hardware_fault
device.unavailable · idempotency.key_conflict
```
