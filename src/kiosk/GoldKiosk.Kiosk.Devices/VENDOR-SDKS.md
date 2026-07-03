# Vendor SDK provisioning — GoldKiosk.Kiosk.Devices real drivers

The real drivers never reference vendor binaries at compile time. Tier-1 devices speak raw
protocols (System.IO.Ports / TcpClient / ClientWebSocket / UdpClient) and need **no vendor
software**. Tier-2 devices load proprietary SDKs by reflection (`Assembly.LoadFrom`) or
late-bound COM (`Type.GetTypeFromProgID`); when the SDK is absent the device connects to
`Faulted` health with detail `vendor_sdk_missing:<name>` — the kiosk keeps running, the
Diagnostics report names the missing piece.

Relative `VendorAssemblyPath` values resolve against the application base directory
(inside the MSIX install folder). Vendor binaries are provisioned onto the machine by the
field-installation runbook — they are **never** committed to this repo and never packaged
into the MSIX (licensing).

## Vendor DLL provisioning table

| Device (key) | Driver | SDK / binary | Source in legacy repo | Install expectation on kiosk |
|---|---|---|---|---|
| Sensor board (`sensor_board`) | `RealSensorBoard` | `Automation.BDaq.dll` (+ Advantech BioDAQ device driver) | `GoldCube.Client/Client.BusinessLogic/HelperDlls/Automation.BDaq.dll` | App base dir (or absolute path in `Connection:VendorAssemblyPath`); Advantech USB-4761 driver installed system-wide |
| Power relays (`power_relays`) | `RealPowerRelays` | same `Automation.BDaq.dll` | same | same (one install serves both drivers) |
| Cash dispenser (`cash_dispenser`) | `RealCashDispenser` | `LibEnvoyAPI.dll` + `IKVM.*.dll` companions; ARCA Envoy RMI service | `GoldCube.Store/ExternalLib/ARCA/2.9.2/` + `GoldCube.Store/ExternalLib/IKVM/` | All DLLs in one folder (pointed at by `VendorAssemblyPath`); Envoy service running on `localhost` |
| ID scanner, Acuant variant (`id_scanner`) | `RealIdScanner` | `Interop.ScanWLightLib.dll` (+ Acuant ScanShell runtime, licence key) | `GoldCube.Store/ExternalLib/libraries/Interop.ScanWLightLib.dll` | App base dir; licence key via config layer 4 (`IdScanner:AcuantLicenseKey`) — legacy key is exposed, rotate |
| ID scanner, Gemalto variant (`id_scanner`) | `RealIdScanner` | `MMMReaderDotNet40.dll` (+ Gemalto full-page reader runtime) | `GoldCube.Store/ExternalLib/Gemalto/Libraries/MMMReaderDotNet40.dll` | Path in `IdScanner:GemaltoAssemblyPath`; reader runtime installed system-wide |
| Fingerprint scanner (`fingerprint_scanner`) | `RealFingerprintScanner` | `Interop.FlexCodeSDK.dll` (+ FlexCode runtime, per-unit serial/verification/activation codes) | `GoldCube.Store/ExternalLib/libraries/Interop.FlexCodeSDK.dll` | App base dir; device codes via config layer 4 — legacy codes are exposed, rotate |
| Camera service (`camera`) | `RealCameraService` | `Camera_NET.dll` (DirectShow wrapper; brings System.Drawing use at runtime) | `GoldCube.Store/ExternalLib/CameraNet/Camera_NET.dll` | App base dir |
| Label printer (`label_printer`) | `RealLabelPrinter` | Brother b-PAC 3 SDK (COM, ProgID `bpac.Document`) + `ShipForm.lbx` template | template: `GoldCube.Store` Dobot folder (`ShipForm.lbx` beside legacy exe); interop reference `Interop.bpac.dll` (not needed — COM late-bound) | b-PAC SDK installed system-wide (COM registration); template at `LabelPrinter:TemplatePath` |
| Metal analyser, InnovX variant (`metal_analyser`) | `RealMetalAnalyserInnovX` | Innov-X COM automation (ProgID TBD — legacy drove it via the out-of-scope `XrfComApplication`) | `GoldCube.Store/XrfComApplication` (needs confirmation) | COM server registered; ProgID in `MetalAnalyser:InnovXProgId`; validated in Phase-4 hardware lab |

No vendor software: scale, tray, volume chamber (serial), robotic arm + AKD axis (TCP),
metal analyser Vanta variant (WebSocket + UDP), bagger (arm-driven).

## Configuration keys added by this ticket (runbook rows for follow-up merge)

All keys live under the `Devices` section and flow through the standard config layering
(package defaults → `kiosk-settings.json` → cloud static config → Key Vault secrets).
Secrets (⚿) must arrive via layer 4 only.

### Per-device connection overrides — `Devices:Overrides:{deviceKey}:Connection`

| Key | Type | Default (legacy parity) | Used by |
|---|---|---|---|
| `Port` | string | scale `COM5`, tray/chamber stepper `COM3` | serial drivers |
| `BaudRate` | int | scale `9600`, stepper `9600` | serial drivers |
| `Host` | string | arm `192.168.1.6`, analyser `192.168.7.2`, dispenser `localhost` | network drivers |
| `TcpPort` | int | analyser `7860` (arm ports 29999/30003/30004 are protocol constants) | network drivers |
| `VendorAssemblyPath` | string | `Automation.BDaq.dll` / `LibEnvoyAPI.dll` / `Interop.ScanWLightLib.dll` / `Interop.FlexCodeSDK.dll` / `Camera_NET.dll` | Tier-2 drivers |
| `Variant` | string | analyser `vanta` (`innovx`), id scanner `acuant` (`gemalto`) | composition root |

### Driver option sections (host binds these beside `DevicesOptions`)

| Section / key | Default | Notes |
|---|---|---|
| `Scale:WeightCommand` / `Scale:ZeroCommand` | `Q` / `Z` | legacy `GetWeightCmd`/`ZeroScaleCmd` |
| `Scale:SettleDelayMs` / `Scale:ZeroSettleDelayMs` / `Scale:ReadTimeoutMs` | 2000 / 3000 / 5000 | read timeout is never infinite |
| `Tray:AckTimeoutMs` / `Tray:MotionTimeoutMs` / `Tray:SensorPollIntervalMs` / `Tray:FallbackTravelDelayMs` | 5000 / 20000 / 250 / 4000 | |
| `VolumeChamber:DeltaV` | 483.4911 | legacy production `MVolume` displacement |
| `VolumeChamber:MtLgChamber` / `CupVolume` / `CalDeltaP` | 735.0 / 50.951 / 10.0 | config-vs-code discrepancy documented on the options class; recalibrate at provisioning |
| `VolumeChamber:CalibrationDeltaTolerance` | 1.0 | new explicit gate for `VolumeReading.Calibrated` |
| `VolumeChamber:AmbientPressurePsi` / `Temperature1` / `Temperature2` | 14.7 / 23 / 23 | |
| `VolumeChamber:SampleCount` | 200 (range 50–400) | plateau averaging |
| `VolumeChamber:CommandDwellMs` / `SealSettleDelayMs` / `PistonSettleDelayMs` / `AckTimeoutMs` | 2000 / 20000 / 47500 / 5000 | |
| `VolumeChamber:PressurePort` / `PressureBaudRate` / `PressureReadTimeoutMs` | `COM4` / 115200 / 3000 | second serial link |
| `RoboticArm:HomePoint` … `BagPoint`, `MidSafePoint*` | legacy point table (`x:y:z:r`) | 8 waypoints |
| `RoboticArm:MotionTimeoutMs` / `FeedbackPollIntervalMs` / `MotionStartGraceMs` | 30000 / 100 / 1500 | motion-complete discipline |
| `RoboticArm:AkdEnabled` / `AkdHost` / `AkdPort` / `AkdAnalyserMotionTask` / `AkdMotionTimeoutMs` | false / `192.168.0.11` / 23 / 0 / 30000 | AKD linear axis |
| `MetalAnalyser:MethodId` / `UserId` | `preciousMetal-VLW` / `Administrator` | |
| `MetalAnalyser:Password` ⚿ | (empty) | Key Vault via device identity only |
| `MetalAnalyser:HeartbeatIntervalMs` / `UdpPort` | 1000 / 7862 | |
| `MetalAnalyser:CommandPacingMs` / `MethodActivationDelayMs` / `ConnectTimeoutMs` / `AnalysisTimeoutMs` | 500 / 2000 / 15000 / 120000 | |
| `MetalAnalyser:BatteryStatusMaxAgeSeconds` | 90 | probe liveness gate |
| `MetalAnalyser:InnovXProgId` | `InnovX.XrfAnalyzer` (placeholder) | confirm in hardware lab |
| `CashDispenser:Denominations` | 100,50,20,10,5,2,1 | major units |
| `CashDispenser:MinimumBillsReserve` | 0 | legacy `MinimumNumberOfBillsRequired` |
| `CashDispenser:MinorUnitsPerMajorUnit` | 100 | |
| `CashDispenser:AssumedCassetteBillCount` | 2000 | F53 reports presence, not counts |
| `CashDispenser:RmiCallTimeoutMs` | 60000 | |
| `Camera:RoleIndexes` | Customer 0, Tray 1, ItemInsert 2, ItemReturn 3, LiveAgent 4 | DirectShow enumeration order |
| `Camera:WarmupDelayMs` | 2000 | |
| `LabelPrinter:TemplatePath` / `ProgId` | `ShipForm.lbx` / `bpac.Document` | |
| `IdScanner:AcuantLicenseKey` ⚿ | (empty) | rotate — legacy key exposed |
| `IdScanner:GemaltoAssemblyPath` | `MMMReaderDotNet40.dll` | |
| `FingerprintScanner:SerialNumber` / `VerificationCode` / `ActivationCode` ⚿ | (empty) | rotate — legacy codes exposed |
| `FingerprintScanner:RegistrationSecret` | `MySecretKey` (legacy literal) | set per tenant at provisioning |
| `FingerprintScanner:CaptureTimeoutMs` | 30000 | |

> Reminder from the configuration standard: adding a key without updating the provisioning
> runbook table is a review blocker — the rows above are staged here for that follow-up merge.
