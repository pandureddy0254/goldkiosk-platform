# Reference Docs Audit — graphify-out + workspace docs/

Date: 2026-07-02
Scope: `../../graphify-out` (legacy code knowledge graph, 2026-05-20 run) and
`../../docs` (the 32-HTML architecture deliverable). Verdict: what is still
authoritative vs superseded by our locked decisions (PG18, platform2 DB baseline,
outbox-only, one MSIX, kiosks→Cloud.Api, Aspire dev-only).

## graphify-out

Graphed the **frozen GoldCube legacy** (Unity UI, Store, Client, PHP api) + the docs
deliverable. Keep as a static reference; **do not regenerate now**. Once this repo has
real code, an AST-only re-graph of the new solution is ~free and useful for coupling
watch — not before.

Still-useful artifacts:
- `master/GRAPH_REPORT.md` + `master/.bridges.json` — the exact DTO/command types crossing
  the legacy kiosk↔store socket (GCCommand/GCCommandResults/BillMix/TierDeduction/…): the
  contract inventory our Contracts package re-expresses.
- Store/GCScripts god-node lists — the anti-pattern checklist (`GCCommandsUIBase` 129
  edges, file-path/log/arm monoliths): what not to rebuild.
- PHP graph: mostly vendored-library noise; discard.

## Workspace docs/ — still-authoritative set (adopt as house standards)

| Doc | Use |
|---|---|
| `api-standards.html` | REST house style: versioning, idempotency keys, cursor pagination, ETags, rate limits, correlation IDs. Adopt as-is. |
| `error-handling-standards.html` | RFC 7807 model + error-code catalogue + retryable-vs-terminal. Adopt as-is. |
| `webhook-design.html` | Signed envelope (HMAC), `domain.action` event catalogue, at-least-once. Adopt as-is for partner webhooks. |
| `authentication-standards.html` | Keep the *schemes* (kiosk mTLS enrollment ceremony, partner OAuth2 client-credentials + HMAC, Argon2id, JWT validation); swap the Entra-as-issuer binding for our issuer decision. |
| `api-connectivity-kit-overview.html` | Three-surface model + regional endpoints/data residency framing for the B2B partner API. |
| `legacy-risk-assessment.html` | The "what the legacy did wrong" charter. Decision-independent. |
| `kiosk-device-architecture.html` | `IDevice<TReq,TResp>` abstraction + device health model — input to the T4 design note. |
| `kiosk-packaging-and-updates.html` | Code-vs-cloud-config cadence split + MSIX rationale. **Collapse its three-package proposal to our one MSIX.** |
| `backend-architecture.html` (pipeline/outbox/idempotency sections) | Concrete Cloud.Api patterns; drop the Service Bus hop. |
| `future-architecture-overview.html` (10 principles + tenancy flow) | Engineering charter; upgrade EF-filter isolation to RLS. |
| `database-schema-complete.html` | Master table catalogue + legacy→new mapping appendix. Reconcile to platform2 baseline + PG18 (doc says PG16/Azure-flavored). |
| `implementation-roadmap.html` (Phase 2 + Phase 4 workstreams/exit criteria) | Mine into the T-plan; the dual-run/AWS-cutover framing is superseded. |

## Superseded (do not cite as authority)

- Anything **SQL Server / Azure SQL** (README stack table, backend/deployment DB
  sections, `new-database-schema.html`, `table-by-table-design.html`).
- **Azure Service Bus** as outbox relay/messaging (outbox drains directly).
- `deployment-architecture.html` Azure topology (Front Door/APIM/Bicep/AWS dual-run) —
  salvage only rollout policy/kill-switch/blue-green/DR framing; provider itself is an
  **open CEO decision** (see `docs/direction/sovereignty-mandate-impact.md`).
- Entra-as-token-issuer statements (issuer decision follows the sovereignty/provider call).
- Three-MSIX packaging; separate Hardware-Agent-only edge model (our Kiosk.Api owns
  devices; file-based durability per ADR 0002).
- Migration-era programme docs (`migration-strategy`, `phase-wise-modernization-plan`).
- `blazor-frontend-architecture.html` kiosk-UI section — kiosk UI is locked Blazor Hybrid
  in this repo's CLAUDE.md; the doc's MAUI framing and tech-stack's Avalonia counter-
  proposal are historical context only.
