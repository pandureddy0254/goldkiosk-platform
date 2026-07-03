using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// A per-device mode override under <c>Devices:Overrides:{deviceKey}</c> — the modernized
/// equivalent of the legacy <c>UseMockCashDispenser</c>-style switches (ADR 0004).
/// </summary>
public sealed class DeviceOverride
{
    /// <summary>The device mode: <c>"Real"</c> or <c>"Mock"</c> (case-insensitive).</summary>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression("^(?i)(Real|Mock)$", ErrorMessage = "Device override Mode must be 'Real' or 'Mock'.")]
    public string Mode { get; set; } = string.Empty;

    /// <summary>
    /// Optional physical-connection overrides for the real driver (COM port, host, vendor SDK
    /// path, variant). Unset members fall back to <see cref="RealDeviceDefaults"/>; the whole
    /// section is ignored while the device runs in <c>Mock</c> mode.
    /// </summary>
    public ConnectionOptions? Connection { get; init; }
}
