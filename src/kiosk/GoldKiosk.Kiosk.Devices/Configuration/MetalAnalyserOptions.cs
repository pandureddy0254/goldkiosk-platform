using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Tuning for the real XRF metal analyser drivers. The fitted gun is selected by
/// <c>Devices:Overrides:metal_analyser:Connection:Variant</c>
/// (<c>vanta</c> — Olympus Vanta WebSocket API — or <c>innovx</c> — legacy Innov-X COM).
/// </summary>
public sealed class MetalAnalyserOptions
{
    /// <summary>Vanta analysis method activated before each test (legacy <c>SetCurrentMethod</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    public string MethodId { get; set; } = "preciousMetal-VLW";

    /// <summary>Vanta login user id (legacy default <c>Administrator</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    public string UserId { get; set; } = "Administrator";

    /// <summary>
    /// Vanta login password. Delivered through config layer 4 (Key Vault via device identity)
    /// — never authored into <c>kiosk-settings.json</c> or committed files.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>UDP heartbeat interval keeping the gun's controller session alive (legacy 1000 ms).</summary>
    [Range(100, 60_000)]
    public int HeartbeatIntervalMs { get; set; } = 1000;

    /// <summary>UDP heartbeat port (legacy <c>VantaUDPPort</c> 7862).</summary>
    [Range(1, 65_535)]
    public int UdpPort { get; set; } = RealDeviceDefaults.VantaUdpPort;

    /// <summary>Pacing delay between protocol commands (legacy inter-command <c>Task.Delay(500)</c>).</summary>
    [Range(0, 10_000)]
    public int CommandPacingMs { get; set; } = 500;

    /// <summary>Dwell after <c>SetCurrentMethod</c> before starting the test (legacy 2000 ms).</summary>
    [Range(0, 30_000)]
    public int MethodActivationDelayMs { get; set; } = 2000;

    /// <summary>Timeout for the handshake/ready exchange after the WebSocket opens.</summary>
    [Range(1000, 120_000)]
    public int ConnectTimeoutMs { get; set; } = 15_000;

    /// <summary>
    /// End-to-end timeout for one analysis run (legacy start-loop allowed 100 s to accept the
    /// start plus open-ended waiting for the final result; the run is now bounded).
    /// </summary>
    [Range(5000, 600_000)]
    public int AnalysisTimeoutMs { get; set; } = 120_000;

    /// <summary>
    /// Maximum age of the last battery-status notification for the gun to be considered
    /// healthy by the probe (legacy <c>VantaBatteryStatusResponseTime</c>, seconds).
    /// </summary>
    [Range(5, 3600)]
    public int BatteryStatusMaxAgeSeconds { get; set; } = 90;

    /// <summary>
    /// COM ProgID of the Innov-X analyser automation object for the <c>innovx</c> variant.
    /// The legacy Innov-X path lived in the out-of-scope <c>XrfComApplication</c>; the ProgID
    /// must be confirmed during Phase-4 hardware-lab validation and set at provisioning.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string InnovXProgId { get; set; } = "InnovX.XrfAnalyzer";
}
