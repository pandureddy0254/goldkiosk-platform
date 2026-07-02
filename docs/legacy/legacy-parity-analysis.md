# Legacy Parity Analysis — GK-1

Date: 2026-07-02
Sources analyzed: `../goldkiosk-platform2/db` (previous platform's Postgres schemas),
`goldkiosk-legacy-code-repos/{MasterProject/Assets/GCScripts, GoldCube.Client,
GoldCube.Store, goldcube-api, Hardware-Component-Documentation, logs}`.
Status: reference document for T2–T14 design notes. Decisions extracted into ADRs 0002–0005.

## 1. End goal

A kiosk running the legacy software today must run the new software with the same
end-to-end transaction capability on the same physical hardware — weigh, photograph,
ID-scan, XRF-assay, volume-check, offer, fingerprint, signature, payout, bag — while the
new platform adds: enterprise packaging (one MSIX, bulk shipping), per-device mock/real
simulation, layered config with cloud override, per-kiosk identity and security, an
explicit outbox for cloud sync, and development best practices per `docs/standards/`.

Two things the legacy does NOT have (new functionality, not parity):
- **Bank-transfer payout at the kiosk** — legacy US flow is cash-dispense only; the India
  flow does bank transfer via a QR-code → customer-phone web journey (Cashfree), never on
  the kiosk itself.
- **Mid-transaction crash resume** — legacy restarts to the attract screen and relies on
  manual/downstream reconciliation.

## 2. Legacy topology (what actually runs on a kiosk)

```
UNITY TABLET (Android, portrait 1080x1920) — UI + TCP *server*
   ▲ TCP 127.0.0.1:9000 (commands) / :9001 (metal rates), JSON, via ADB port-forward
   │ (PC connects OUT to the tablet — inverted vs our design)
GoldCube.Store.CommandServer.exe (WinForms) — DI (Castle Windsor), command executors,
   │ interceptors (transaction guard, server-API calls, camera, analyser)
   ├─ launches + drives sidecar EXEs via PostMessage(UM_*) + named EventWaitHandle + files:
   │    XRFMetalAnalyser.exe (Innov-X COM)  │ Client.exe (arm/scale/chamber sidecar)
   │    CameraApplication.exe               │ CashDispenserApplication.exe (Fujitsu F53/ARCA Envoy)
   │    FPScanner.exe (FlexCode)            │ HLNonBlockingIDScanner.exe (3M/Gemalto)
GoldCube.Store.Monitor.exe — watchdog (keeps CommandServer + Client alive)
Backend: raw PHP at thegoldcube.com (single POST endpoint, action ladder) + MySQL RDS
Side channels: RabbitMQ (finished/aborted/zipperFailed queues), AWS S3 (images), OpenTok
(live-agent video KYC), Pusher, SMTP.
```

New-platform mapping: CommandServer + all sidecars + Client.exe collapse into
**GoldKiosk.Kiosk.Api + GoldKiosk.Kiosk.Devices** (one process, in-process drivers behind
ports). The Unity tablet becomes **GoldKiosk.Kiosk.UI** (Blazor Hybrid) as a *client* of
Kiosk.Api (REST + SignalR). Store.Monitor becomes the MSIX shell watchdog. The PHP backend
becomes **GoldKiosk.Cloud.Api**. WorkFlow.Service is vestigial (empty XAMLX; nothing
references it) — dropped.

## 3. Transaction flow (parity target)

19 screens, driven by panel-to-panel handoff in the Unity UI; authoritative state lives
server-side in `GCTransaction` (`ItemState`: None → InTray → UnderLACamera → UnderXRAY →
OnScale → InVolumeChamber → Bagged/Returned/OfferGenerated).

Happy path: Home(attract) → FAQ → Start(Sell/Pawn, Gold/Silver) → T&C → Selfie → ID scan →
PleaseNote → Item description (type/size/style) → Open tray → Close tray → Analysis
(weight + XRF + volume + live-agent item check) → Offer → [sell: Fingerprint → Signature |
pawn: Contract(3 checkboxes + initials) → Fingerprint → Signature] → Email → Phone →
GetCash → Dispense → ThankYou → attract.

Branches/aborts: every KYC failure (not-govt-ID, underage, expired, blacklisted,
face-mismatch), every analysis rejection (empty tray, multiple items, unidentified, wrong
type, weight 0 / <1g, gold-plated, insufficient purity), offer rejection, timeouts (idle →
30s countdown → REGISTER_TIMED_OUT → return item), after-hours schedule, store outage,
insufficient cassette cash. Full EN/ES error catalogue with recovery routing lives in
`ErrorDataManager` (Types 1–8) — port as a localized error/recovery table.

Timeout layers to reproduce: per-screen idle budget (+45s grace) → 30s countdown panel →
server-side abort + item return; 300s hard reset if abandoned on the countdown.

## 4. The command contract (the Kiosk.Api surface)

The complete UI↔hardware vocabulary is 41 commands / ~100 result codes
(`GCCommands.cs` / `GCCommandResults.cs` in both MasterProject and GoldCube.Store — they
must stay in sync; in the rebuild they become one Contracts type set):

- Transaction: TRANSACTION_INIT, ADD_ITEM_INIT, SET_ITEM_DETAILS, ACCEPT_OFFER,
  REJECT_OFFER, TRANSACTION_REFUND, REGISTER_TIMED_OUT
- Identity/KYC: ID_SCANNER_SCAN, UPLOAD_CUSTOMER_IMAGE, SET_EMAIL, SET_PHONE_NUMBER,
  SET_SIGNATURE, UPLOAD_SIGNATURE_AND_SIGNED_TC, FINGER_PRINT_SCANNER_SCAN/CONTINUE/CANCEL
- Analyser/arm: METAL_ANALYZER_TRAY_OPEN / TRAY_CLOSE / TRAY_CLOSE_WITHOUT_ITEM /
  GET_WEIGHT / GET_PROGRESS (polled) / RETURN_ITEM / TRAY_MOVE_UNDER_LA_CAMERA /
  TRAY_CONTINUE_ANALYSIS
- Cash: CASH_DISPENSER_GET_DENOMS, CASH_DISPENSER_DISPENSE
- Live agent: LIVE_AGENT_ID_APPROVAL_STATUS_CHECK, LIVE_AGENT_PERFORM_ITEM_CHECK
- Crypto (feature-flagged): CRYPTO_FOR_GOLD_REQUEST, CRYPTO_TRANSACTION_DETAILS,
  RECEIVE_CRYPTO_REVERSE_TRANSACTION, SECURITY_CODE_DETAILS, RESEND_SECURITY_CODE
- System: GET_DISPENSER_ID, SYSTEM_HEALTH_CHECK, SYSTEM_SHUTDOWN, CHECK_STORE_STATUS_ONLINE,
  GET_RATES, BAGGER_GET_STATUS

Legacy wire quirks NOT to copy: JSON-string-inside-JSON envelope, single-socket 1 MB
frames, UI-as-server + ADB port-forward, last-panel-wins response dispatch. The rebuild
exposes the same *vocabulary* as versioned REST endpoints + SignalR events with proper
correlation.

## 5. Device inventory (ports to define in Kiosk.Devices)

| Device | Legacy driver | Interface | Notes for port design |
|---|---|---|---|
| Scale | `Scale.cs` (Client) | RS-232 COM5 9600 8-N-1, `Q`/`Z` ASCII (MT-SICS-style) | brittle `Substring(3,11)` parse + culture bug — fix; 2s settle |
| XRF (new) | `GcMetalAnalyserUsingVantaApi` | Olympus Vanta: WebSocket ws://192.168.7.2:7860 + UDP heartbeat :7862, JSON | login + method `preciousMetal-VLW`; no cal-check; power-cycle via arm relay |
| XRF (old) | `XrfComApplication` (VB) | Innov-X COM SDK via sidecar EXE + PostMessage | Standardize() cal-check path; both behind one `IMetalAnalyser` |
| Robot arm | `DobotManager` | Dobot: TCP 192.168.1.6 :29999/:30003/:30004 (or COM7 Magician + `.playback` teach files) | fire-and-forget MovJ + blind waits — add motion-complete discipline |
| Linear axis | `TCPControl` | Kollmorgen AKD servo, telnet 192.168.0.11:23 ASCII | drv.en / mt.move / motionstat polling |
| Volume chamber | `GoldCube.Client` | pressure sensor COM4 115200; stepper board COM3 9600 (`cu/cd/pu/pd`, tray `ld/lu`) | Boyle's-law math + chamber constants: port verbatim, reconcile config-vs-code constant drift |
| Sensors/power | `DoInput`/`DiOutput` | Advantech USB-4761 DAQ | bit map: tray, chamber, UPS, chamberCup, scaleCup; power relays incl. Vanta gun |
| Camera(s) | `CameraApplication` + Unity WebCamTexture | DirectShow (Camera_NET); camera indices from config | customer selfie, item entering/exiting, live-agent, NTEP feed |
| ID scanner A | `GCIdScanner` | Acuant ScanShell (in-process SDK) | extracts full PII incl. SSN |
| ID scanner B | `HLNonBlockingIDScanner` | 3M/Gemalto full-page reader (visible/IR/UV) | selected by `UseGemaltoIDScanner` |
| Fingerprint | `FPScanner.exe` | FlexCodeSDK (serial+activation codes) | 3-strike retry rule |
| Cash dispenser | `CashDispenserApplication` | Fujitsu F53 via ARCA Envoy (Java RMI / IKVM) | greedy bill-mix, per-cassette verify, 3 dispense modes |
| Label printer | `BrotherPrinter` | Brother b-PAC, template `.lbx` | fired at dispense; fields: Offer, InvoiceId, BagNumber, Weight, Karat |
| Inclinometer | `Inclinometer` | Level Developments LCP (USB HID) | rig-level check |
| UPS | DAQ input bit | — | presence bit only |
| Bagger | via arm + status cmd | — | interlock: no new transaction while bagging |

Mock precedent to carry forward: `UseMockDevices` swaps every device for `GCMock*` in DI,
with per-device overrides (`UseMockCashDispenser`, `UseMockIdScanner`, `UseMockServerApi`,
`UseMockMQ`) — `WindsorRegistrar.cs`. In the rebuild this becomes per-device
`Mode: Real|Mock` in layered Options config (ADR 0004).

## 6. Transaction folder dump format (the durability contract — ADR 0002)

Canonical source: `GCFileNamePathProvider.cs`, `GCTransaction.cs` (GoldCube.Store).

- Root: config `CustomerDataFolder` (legacy `C:\GoldCubeCustomerData`; new
  `%ProgramData%\GoldKiosk\transactions\` with the same inner layout, root configurable).
- Per transaction: `{root}\{dd-MM-yyyy}\{HH-mm-ss}\` containing:
  - `transactionDetails.json` — the full record. Top level: StoreTransactionId (GUID),
    DispenserId, StoreId, LicenseNo, Customer{Phone, First, Last, DOB, Email},
    FingerPrintHash, IsRefund, Items[] {Temperature, Weight, DWTWeight, Karat,
    MetalPercentage, MarketPrice, TierPricePerDWT, UnitPricePerDWT, Impurities,
    OfferPrice, OfferAccepted, OfferId, Payout, MetalType, BagNumber,
    ImageProcessingDir, OfferMadeOn, CustomerRespondedToOfferOn}, TotalPayout,
    CashDetails{One..Hundred}, IsPawn, PawnContract*, IsCrypto, CryptoCurrencySelected.
  - `offerDetails_{itemNo}.json`, `transactionLog.txt`
  - Images: `customerImage.png/.jpg`, `enteringItemImage_N.bmp`, `exitingItemImage_N.bmp`,
    `itemImageForLiveAgent.*`, `licenseImage.*`, `licensePhotoImage.*`, `signImage.png`,
    `signedTC.png`, `fingerPrint.png`, `FP_SCAN\` (templates/flags)
  - Numbered image-processing subfolders `{N}\` (tray captures, Blobs/Canny/BW,
    `ElementAnalysis.txt`)
- Schema is preserved 1:1 (field names and casing) so downstream tooling and operator
  habits survive cut-over. PII handling inside the folder is hardened per ADR 0002
  (legacy stored SSN/biometrics in plain text — the new store must not).
- The legacy `HARDWARE\*.txt` / `XRF_MA\` / `CAMERA\` etc. folders are an IPC mailbox,
  not durable data — replaced by the Kiosk.Api in-process calls; not reproduced.

Cloud sync: legacy posts are fire-and-forget (`.Result` + try/catch) with RabbitMQ
finished/aborted messages and a downstream "zipper/recycler" re-uploading folders. The
rebuild replaces this with the explicit file-backed outbox (idempotency keys, retry,
reconciliation) — an upgrade the schema set already supports (`audit.outbox_messages`,
`audit.inbox_messages`, `payout_attempts.idempotency_key`).

## 7. Business rules to port into Domain (harvested, with legacy sources)

Pricing/offer (goldcube-api `calculate_price`, `metal_rate.php`; Store `TierDeduction`):
- Per-gram rate per karat = `round(askPrice_troyOz * karatFactor / 31.1, 4)`.
- Live karat factors (margin baked in): g8-9=0.312, g10=0.39, g12=0.46, g14=0.55,
  g18=0.71, g22=0.87, g24=0.98, silver=1, platinum=1. (True purities: 24k=0.99,
  22k=0.916, 18k=0.75 — kept commented in legacy; new Domain must separate purity from
  margin explicitly.)
- `sale_price = weight_g * rate[karat]`; `offer = floor(sale_price * store_offer_pct/100 / 5) * 5`
  (round DOWN to nearest 5; Trinidad path converts currency then rounds to nearest 5).
- UI tier display: `tierPrice = market * tier% * 0.70 / (100*20)` — 70% payout factor,
  DWT = 1/20 oz. Impurity% = (Unit−Tier)/Unit×100.
- Sale vs pawn use different offer percentages; pawn offer carries APR, MonthlyFee,
  TotalRepayment, DueDate.

Assay/karat (`GCKaratCalculator`, `ElementMap.txt`):
- Two karat computations: density-weighted (`KaratUsingDensity`) and raw XRF Au%
  (`KaratUsingXrfPercentage`); selection by config flag. CalculatedVolume =
  weight / Σ(density_i × pct_i).
- Rejections: W/Pt/Ir/Ru/Pd/Pb/Mo/Bi/Cd/In above threshold; Rh>4%; Fe>10%; Mn;
  gold-plated flag; karat < minimum; silver% < minimum.
- Fraud/volume cross-check: measured (pycnometer) vs calculated volume; correction factor
  band 65–89, else reject `UnacceptableVolumeError` above weight threshold.
- Volume math (GoldCube.Client `MVolume`): Boyle's-law two-pressure
  `V = ΔV / (1 − p1·t2/(p2·t1))` minus cup volume; 50–400 sample averaging. Port verbatim.

Eligibility/KYC:
- Government ID required; age ≥ 18; not expired; blacklist (`bad` flag) blocks; Azure Face
  match confidence > 0.25 (legacy value — revisit); live-agent approve/amend loop.
- India regime: Aadhaar OTP, PAN, bank verification via Cashfree Secure ID; name-match
  score = first×0.4 + middle×0.2 + last×0.4 (100 on exact first+last).
- No transaction limits / CTR / velocity checks exist in legacy — compliance rules are a
  NEW requirement, not parity (the platform2 `compliance` schema has the tables).

Kiosk-side rules:
- Weight 0 → no item; 0 < w < 1g → reject (<1 gram not accepted).
- Fingerprint: 3 failures → abort + return item.
- Bagger interlock; low-cash gate (`MinimumAmountAvailableToStartTransaction`);
  cassette-map-mismatch gate; insufficient-cash offer rejection;
  MaximumNumberOfBillsCanBeDispensed.
- Bill mix: highest-denomination-first greedy over cassettes; per-cassette verification.
- Bag number: monotonic per-machine sequence (legacy: registry-backed).
- Invoice number: `{countryCode}{2-char company}-{6-digit seq}` (server-side, gap-less —
  platform2 `doc.invoice_numbers` reproduces this).
- Validation quirks kept for parity review: email = contains `@` and `.`; phone = 10
  digits, reject if any digit repeats > 7 times. (Recommend proper validators + these as
  minimums.)
- Settlement === dispense in legacy (no separate authorization step). Refund is
  per-invoice, `IsRefund=true` + `MakeRefund` per invoice.

## 8. Cloud backend capability inventory (Cloud.Api / AdminPortal / CrmPortal scope)

Legacy PHP is one POST endpoint with an `action` ladder + one global shared key
(`md5('theGoldCube')`) — replaced wholesale by versioned Minimal APIs + per-kiosk device
identity (ADR 0003). Capabilities to reproduce:

- Kiosk-facing: offer creation (GET-OFFER[-INR]), settlement (MAKE-SELL[-INR],
  MAKE-CRYPTO-SALE), refunds, ID-scan upload + approval polling, item image upload,
  cassette/bills query, metal rates, store status/outage, heartbeat/telemetry (battery),
  screen/status reporting, alert raise, invoice email, karat-range config pull, APK/OTA
  pull + install confirmation (→ becomes MSIX ring assignment in the rebuild).
- Customer web (QR journey, India): short-URL resolve, Aadhaar OTP/PAN/bank verification,
  document upload, beneficiary + transfer creation (Cashfree Payouts; transfer idempotency
  by token), payout webhook (unsigned in legacy — must be signed/verified).
- Admin/live-agent: ID approve/amend, transaction monitoring, video KYC (OpenTok),
  customer edit + blacklist + taxable flags, fleet management (dispenser↔store mapping,
  karat range, store margin %, bankroll), OTA publish, maintenance mode, cassette levels,
  battery telemetry, email alerts.
- Integrations: price feeds (Xignite SOAP, metals-api REST) → `pricing.metal_rates`;
  Cashfree (payouts + Secure ID KYC); Coinbase + internal crypto service (feature-flagged);
  SMTP; S3 → Azure Blob; OpenTok/Pusher → re-evaluate (SignalR + a video provider).
- Config-to-kiosk is pull/poll in legacy (APK, karat range, status, bills) — formalized in
  the rebuild as the versioned cloud config document with ETag caching (config layer 3).

## 9. Database baseline (platform2 `db/` verdict — ADR 0005)

Adopt the platform2 schema sets as the starting point (admin-dashboard → `goldkiosk`;
crm → `goldkiosk_crm`). They are thin-client-shaped (no kiosk-local DB assumptions),
snake_case, EF-friendly (text+CHECK enums), with outbox/inbox, event-sourced tx tables,
PII encryption columns + blind-index hashes, RBAC templates, activation keys, partitioned
heartbeats/events/logs.

Fixes required on adoption (all found by analysis):
1. Child-table tenant isolation: many tables without `tenant_id` have no RLS — add
   `tenant_id` (or EXISTS-join policies) to customer/tx/payment/kyc child tables.
2. RLS must be the last structural step (or idempotently re-run): `0802`/`0803` tables
   created after the `0600` RLS pass have no policy.
3. Global-default rows (`tenant_id IS NULL` screensavers/terms) are invisible under the
   equality policy — needs `OR tenant_id IS NULL` for reference tables or a DEFINER fn.
4. Guard GUC cast: `NULLIF(current_setting('app.tenant_id', true), '')::uuid` everywhere.
5. Partition `audit.audit_events`; exclude large/encrypted columns from audit capture.
6. Drop redundant `0034`; complete or drop the `reporting.expenses_source` placeholder.
7. Add for the thin-client kiosk model: kiosk command queue (cloud→kiosk, acked),
   kiosk applied-config-version tracking, idempotency keys on kiosk-originated events.
8. Adopt the CRM-style per-operation + RESTRICTIVE-deny RLS and DEFINER-RPC write paths
   for sensitive admin tables as well.

## 10. Parity gaps and open scope decisions

| # | Gap | Owner decision |
|---|---|---|
| 1 | Bank-transfer payout at kiosk does not exist in legacy (QR→phone journey only) | Confirmed direction: bank transfer is the new primary payout; cash remains hybrid fallback. Kiosk-side UX is new design. |
| 2 | Crash/mid-transaction resume absent in legacy | New: Kiosk.Api restart-safe session state machine (resume or safe-abort with audit) per architecture principles §4. |
| 3 | No offline retry/queue for cloud posts | New: file-backed outbox with idempotency keys (ADR 0002). |
| 4 | No compliance limits/reporting in legacy | New: implement against platform2 `compliance` schema; rules TBD per market. |
| 5 | Legacy stores PII (SSN, DOB, biometrics) in plaintext on disk + DB | New store encrypts per security standard; folder parity is structural, not a licence to store plaintext PII. |
| 6 | Crypto-for-gold flows exist in legacy | Stays feature-flagged off by default (workspace decision). |
| 7 | Live-agent video KYC (OpenTok) | Capability retained in scope; provider choice needs a design note. |
| 8 | Entertainment area (games/WooCommerce store) | Out of scope for parity v1. |
| 9 | NTEP legal-for-trade display | Config-gated in legacy (`isNtepActive`); keep as feature flag; note doc'd absence of metrology compliance on the scale path. |

## 11. Security debt register (never carry forward; rotate everything)

Treat every credential in the legacy repos as exposed. Locations (values not recorded):
- `goldcube-api/api/_inc/config.inc.example`: production RDS host+password, SMTP creds,
  Cashfree test+prod client id/secret, metals-api key, static AES-256 key+IV (weak, known
  value — all legacy `*_hash` KYC fields are effectively reversible), SALT.
- Global kiosk API key derivable: `md5('theGoldCube')`; embedded in addDispenser.php,
  swagger.php, Unity `LAStatusManager`.
- `GoldCube.Store` `ConfigProvider.cs` hardcoded fallbacks: Slack tokens, Vanta
  credentials, Azure Face API key, RabbitMQ guest/guest, Firebase URL, Runscope key.
- `GoldCubeAppSettings.xml`: GoldCubeApiKey, Acuant license, FlexCode FP scanner
  serial/verification/activation codes.
- Unity `EmailHandler.cs`: hardcoded Gmail SMTP credentials.
- Hardware docs: Vanta default user/password in config tables.
- PII debris: `api/newsqlfile.txt` (real DL numbers), `newfile.txt`, `statusfile.txt`,
  `coincallback.txt`; plaintext `be_customers` PII; unencrypted biometrics in S3 and in
  transaction folders.
- Systemic legacy flaws not to replicate: SQL injection throughout (string-concatenated
  raw SQL), CORS `*`, unsigned webhooks (Cashfree payout, Coinbase), one shared static
  API key for the whole fleet, live-agent failure path that fabricates a fake approved
  identity (`ServerApiInterceptor.cs:459-471`).

## 12. Legacy → new solution map

| Legacy | New |
|---|---|
| Unity MasterProject (GCScripts) | GoldKiosk.Kiosk.UI (Blazor Hybrid) |
| CommandServer + executors + interceptors | GoldKiosk.Kiosk.Api (endpoints + pipeline behaviors) |
| GCTransaction / ItemState | GoldKiosk.Kiosk.Core session state machine |
| Device sidecar EXEs + GoldCube.Client | GoldKiosk.Kiosk.Devices (ports + real drivers + simulators) |
| GCCommands/GCCommandResults enums | GoldKiosk.Contracts (REST routes + SignalR events + ProblemDetails types) |
| CustomerDataFolder dumps | Kiosk.Api transaction folder store + outbox (ADR 0002) |
| Store.Monitor watchdog | MSIX shell (Kiosk.UI) watchdog per packaging standard |
| WorkFlow.Service | dropped |
| goldcube-api PHP | GoldKiosk.Cloud.Api |
| Admin panel / live-agent CRM | GoldKiosk.Cloud.AdminPortal |
| platform2 db/admin-dashboard | `goldkiosk` PostgreSQL database (ADR 0005) |
| platform2 db/crm | `goldkiosk_crm` PostgreSQL database (ADR 0005) |
| GoldCubeAppSettings.xml + Config.txt | kiosk-settings.json + cloud config layers (ADR 0004) |
| RabbitMQ + zipper/recycler | file-backed outbox + reconciliation (ADR 0002) |
| APK OTA (be_apkconfig) | MSIX .appinstaller rings (packaging standard) |
