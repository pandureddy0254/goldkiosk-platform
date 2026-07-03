# GoldKiosk Machine Provisioning Runbook — Configuration Key Registry

> **This file is the binding configuration key registry** required by
> `docs/standards/configuration-and-operations.md` §1: every configuration key a kiosk
> binds must have a row in the table below. **Adding a key without updating this table is
> a review blocker.** Layer names follow the standard's layering model
> (package default → machine `kiosk-settings.json` → cloud config → cloud secret), where
> later layers win.
>
> GK-2 scope note: the kiosk vertical currently ships every non-machine key as a package
> default in `appsettings.json`; the cloud-config and secret layers are wired in a later
> ticket. The **Layer** column records each key's authoritative home so provisioning and
> future layering land correctly. No GK-2 key is a secret — secrets never appear in
> `kiosk-settings.json` or the package.

## Configuration keys (GK-2 kiosk vertical)

| Key | Layer (package default / machine kiosk-settings / cloud config / secret) | Example | Refresh |
|---|---|---|---|
| `Kiosk:KioskId` | machine kiosk-settings | `GK-DEV-01` | provisioning / field tech |
| `Kiosk:StoreId` | machine kiosk-settings | `STORE-DEV` | provisioning / field tech |
| `Kiosk:TransactionRoot` | machine kiosk-settings | `%ProgramData%\GoldKiosk\transactions` (dev: `./data/transactions`) | provisioning / field tech |
| `Kiosk:OfferTtlSeconds` | cloud config (package default until layered) | `600` | config poll / on-demand push |
| `Kiosk:IdleTimeoutSeconds` | cloud config (package default until layered) | `90` | config poll / on-demand push |
| `Kiosk:TermsVersion` | cloud config (package default until layered) | `2026-06-01.v3` | config poll / on-demand push |
| `Features:PawnEnabled` | cloud config (package default until layered) | `true` | config poll / on-demand push |
| `Features:CryptoEnabled` | cloud config (package default until layered) | `false` (opt-in per tenant) | config poll / on-demand push |
| `Features:FingerprintRequired` | cloud config (package default until layered) | `false` | config poll / on-demand push |
| `Features:PayoutMethods` | cloud config (package default until layered) | `["cash", "bank_transfer", "debit_card"]` | config poll / on-demand push |
| `Analysis:MinWeightGrams` | cloud config (package default until layered) | `1.0` | config poll / on-demand push |
| `Analysis:MinGoldPercent` | cloud config (package default until layered) | `33.3` | config poll / on-demand push |
| `Analysis:VolumeTolerancePercent` | cloud config (package default until layered) | `25.0` | config poll / on-demand push |
| `MockRates:GoldPerGram` | package default (dev-only mock; live rates arrive with Cloud.Api) | `108.94` | with app update |
| `MockRates:SilverPerGram` | package default (dev-only mock) | `1.32` | with app update |
| `MockRates:Currency` | package default (dev-only mock) | `USD` | with app update |
| `MockRates:StoreMarginPercent` | package default (dev-only mock) | `70.0` | with app update |
| `MockRates:GoldChangePercent` | package default (dev-only mock) | `0.42` | with app update |
| `MockRates:SilverChangePercent` | package default (dev-only mock) | `-0.11` | with app update |
| `Devices:DefaultMode` | machine kiosk-settings (hardware fact, ADR 0004) | `Mock` (dev) / `Real` (production machine) | provisioning / field tech |
| `Devices:Overrides:{key}:Mode` | machine kiosk-settings (per-device override, ADR 0004) | `Devices:Overrides:scale:Mode = Real` | provisioning / field tech |
| `Devices:Simulation:LatencyMultiplier` | package default (dev/simulator only) | `1.0` | with app update |
| `Devices:Simulation:FaultDevices` | package default (dev/simulator only) | `[]` (e.g. `["dispenser"]` to fault-inject) | with app update |
| `KioskUi:ApiBaseUrl` | machine kiosk-settings (Kiosk.Api port is a machine fact; loopback only) | `http://localhost:5201` | provisioning / field tech |
| `KioskUi:Locale` | machine kiosk-settings | `en-US` | provisioning / field tech |
| `KioskUi:DoneScreenSeconds` | package default (UI default) | `15` | with app update |
| `KioskUi:IdleGraceSeconds` | package default (UI default) | `30` | with app update |
| `KioskUi:AttractWordCycleMs` | package default (UI default) | `2500` | with app update |
| `Serilog` (section: `MinimumLevel`, `WriteTo`, `Enrich`) | package default (local-first logging; sinks/levels per configuration-and-operations §4) | file sink `./logs/kiosk-api-.log`, daily rolling, 30 days | with app update |

## Provisioning steps (summary)

The full machine provisioning procedure (Windows baseline, kiosk account with Assigned
Access, trust chain, device identity enrollment, network allow-list, appinstaller ring
install, Diagnostics sign-off) is defined in
`docs/standards/packaging-and-deployment.md` §4 and is generated per release. For GK-2
dev machines only steps relevant to configuration apply:

1. Author the machine's `kiosk-settings.json` (dev: `appsettings.Development.json`
   overrides) from the key table above — machine-layer keys only, never secrets.
2. Verify the kiosk fails fast on invalid configuration: all options classes bind with
   `ValidateDataAnnotations().ValidateOnStart()`.
3. Confirm `Kiosk:TransactionRoot` is writable by the kiosk account; the per-day
   transaction folders, `counters/` (invoice sequence), and journals live beneath it.
