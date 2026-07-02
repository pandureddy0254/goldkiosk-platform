# AI Item Verification & Live-Agent Escalation (GK-2 addendum)

Date: 2026-07-02 · Status: Binding. Sources: legacy mining (goldcube-api, GoldCube.Store,
GCScripts) + platform2 AI services.

## Model

**AI-first, agent-escalation, fail-closed identity.**

State model: `accepted_by_ai` → happy path · `rejected_ai(reason)` (empty_tray |
multiple_items | unidentified | unaccepted_type) · `escalated_pending_agent` ·
`agent_approved` · `agent_rejected(reason)` · `timed_out_return_item`.

Rules (learned from legacy failures — never reproduce):
1. **Item check** may degrade-open only on AI *transport* failure (accepting a real item is
   low-risk); a low-confidence or rejected verdict escalates to an agent, it is never
   silently approved. Legacy hard-forced `LIVE_AGENT_ITEM_CHECK_APPROVED` and commented
   out the agent poll — that pattern is banned.
2. **Identity/KYC is fail-closed.** Legacy fabricated a synthetic identity on API failure
   and set `IsApproved=true` unconditionally, with face-match confidence 0.25. The new
   flow: any identity-step error → retry → abort with item return. Face-match threshold is
   config, defaulted conservatively; no auto-approve path exists in code.
3. Agent timeout (no agent joins within TTL) → `timed_out_return_item`, never approve.
4. **Image separation**: tray/item camera frames live in a separate store from
   selfie/ID/PII images (legacy conflated both in one table + S3 prefix). Only tray
   images ever reach the AI vision endpoint. Offer-explain sends only the formatted
   amount + optional item one-liner — keep that data minimization.

## Endpoints (Cloud.Api — kiosks talk only to Cloud.Api)

- `POST /api/v1/items/analyze` — Claude vision (multipart: category + images). Carries
  forward from platform2 with additive fields: `requires_live_agent`, `confidence`.
  Structured output: `{accepted, detected_category, reject_reason}`.
- `POST /api/v1/offers/explain` — carries forward as-is.
- `POST /api/v1/live-agent/item-review` (replaces UPLOAD-ITEM-IMAGE) — kiosk uploads
  tray image on escalation; creates a pending review; notifies agents (SignalR).
- `GET  /api/v1/live-agent/item-review/{id}` (replaces ITEM-APPROVAL-STATUS) —
  `{status: pending|approved|rejected, reason?}`; polled or pushed. No forced-true.
- `GET/POST /api/v1/live-agent/availability` (replaces GETWORKING/UPDATE-WORKING).
- Agent verdict endpoint lives in the AdminPortal surface (replaces IDAPPROVE) —
  written by agents, never the kiosk.
- Real-time: SignalR replaces hardcoded Pusher; video provider decision open (OpenTok
  legacy → evaluate Twilio/ACS when live video lands).

Kiosk.Api mediates: `POST /sessions/{id}/agent/item-check` on the edge maps to the
Cloud.Api escalation endpoints; the kiosk UI only sees `agent_status` SignalR events.

## Rotate list additions (hardcoded in legacy source; revoke)

- Pusher app 311496 key/secret (goldcube-api PHP :2413/:4979)
- Azure Face API subscription key (`ConfigProvider.cs:253`)
- The third-party `goldkiosk-ai.smartlawyer.ai` dependency is retired in favor of the
  in-house Cloud.Api → Claude path.
