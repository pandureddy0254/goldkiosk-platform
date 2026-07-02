namespace GoldKiosk.Kiosk.Devices.Abstractions;

/// <summary>
/// Canonical device keys used for registry lookup, configuration overrides
/// (<c>Devices:Overrides</c>, matched case- and underscore-insensitively), health reporting,
/// fault injection, and diagnostics. Keys are snake_case and stable — they appear on the wire
/// (device health events, probe endpoints) and in telemetry.
/// </summary>
public static class DeviceKeys
{
    /// <summary>Precision scale (legacy: MT-SICS ASCII over RS-232 COM5).</summary>
    public const string Scale = "scale";

    /// <summary>XRF metal analyser (legacy: Olympus Vanta WebSocket / Innov-X COM).</summary>
    public const string MetalAnalyser = "metal_analyser";

    /// <summary>Robotic arm incl. linear axis (legacy: Dobot TCP + Kollmorgen AKD).</summary>
    public const string RoboticArm = "robotic_arm";

    /// <summary>Volume measurement chamber (legacy: pressure COM4 + stepper COM3).</summary>
    public const string VolumeChamber = "volume_chamber";

    /// <summary>Digital-input sensor board (legacy: Advantech USB-4761 DAQ inputs).</summary>
    public const string SensorBoard = "sensor_board";

    /// <summary>Power relay outputs (legacy: Advantech USB-4761 DAQ outputs).</summary>
    public const string PowerRelays = "power_relays";

    /// <summary>Role-keyed camera service (legacy: DirectShow, index per role).</summary>
    public const string Camera = "camera";

    /// <summary>Government ID scanner (legacy: Acuant ScanShell / Gemalto full-page).</summary>
    public const string IdScanner = "id_scanner";

    /// <summary>Fingerprint scanner (legacy: FlexCode SDK).</summary>
    public const string FingerprintScanner = "fingerprint_scanner";

    /// <summary>Cash dispenser (legacy: Fujitsu F53 via ARCA Envoy).</summary>
    public const string CashDispenser = "cash_dispenser";

    /// <summary>Bag label printer (legacy: Brother b-PAC template printing).</summary>
    public const string LabelPrinter = "label_printer";

    /// <summary>Bagging unit (arm-driven, status-command interlock).</summary>
    public const string Bagger = "bagger";

    /// <summary>
    /// Every known device key, in the canonical registration order. Hosts use this to
    /// validate configuration override keys and to compose the full device set.
    /// </summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Scale,
        MetalAnalyser,
        RoboticArm,
        VolumeChamber,
        SensorBoard,
        PowerRelays,
        Camera,
        IdScanner,
        FingerprintScanner,
        CashDispenser,
        LabelPrinter,
        Bagger,
    ];
}
