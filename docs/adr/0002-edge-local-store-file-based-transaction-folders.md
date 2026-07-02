# ADR 0002 — Edge local store: file-based transaction folders, no local SQL

Date: 2026-07-02
Status: Accepted (owner decision, 2026-07-02)

## Context

The architecture principles left the kiosk-local durability store open ("SQLite/file
store per ADR"). The owner has decided: **no SQL database runs on the kiosk.** The legacy
platform already persists every transaction as a per-transaction folder of JSON + images
(`C:\GoldCubeCustomerData\{dd-MM-yyyy}\{HH-mm-ss}\`, schema in
`docs/legacy/legacy-parity-analysis.md` §6), and downstream tooling, operator habits, and
support processes are built around that shape.

## Decision

- `GoldKiosk.Kiosk.Api` persists every customer transaction as a **per-transaction
  folder** under `%ProgramData%\GoldKiosk\transactions\{dd-MM-yyyy}\{HH-mm-ss}\`
  (root configurable via `kiosk-settings.json`), preserving the legacy inner layout and
  the `transactionDetails.json` field names/casing 1:1.
- The folder is written **before** any cloud call (local durability first). Writes use
  write-temp-then-rename so a crash never leaves a torn record.
- The **outbox is file-backed**: sync state (pending/sent/acked + idempotency key +
  attempt count) lives in a small journal alongside the transaction folder, replacing the
  legacy RabbitMQ finished/aborted queues and the external "zipper/recycler".
  Edge→cloud posts carry idempotency keys; reconciliation compares outbox vs cloud ledger.
- **PII hardening is layered on top of the legacy shape**: the structure is preserved,
  but sensitive content (ID numbers, biometrics) is encrypted at rest per the security
  standard — structural parity does not grandfather plaintext SSNs.
- Retention: size-capped, oldest-first pruning with audit events, per the
  configuration-and-operations standard.

## Consequences

- No SQLite/EF on the edge; Kiosk.Core's queue/staleness/session state persists to files.
- Drop-in parity for operators and any tooling that reads transaction folders.
- Crash-recovery and at-least-once delivery tests (testing standard §Financial) target
  the file journal instead of a DB.
- If multi-kiosk-per-store concurrency or query needs ever outgrow files, revisiting this
  ADR is required — do not quietly introduce a local DB.
