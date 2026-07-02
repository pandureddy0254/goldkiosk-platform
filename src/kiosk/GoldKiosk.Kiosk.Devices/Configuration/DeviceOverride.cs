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
}
