using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Role-to-physical-camera mapping for the real DirectShow camera service. Keys are
/// <c>CameraRole</c> names (case-insensitive), values are DirectShow device indexes in
/// enumeration order — the legacy per-machine <c>CaptureImage(int index)</c> wiring.
/// </summary>
public sealed class CameraServiceOptions
{
    /// <summary>Camera index per role. Roles absent from the map fail capture with a clear reason.</summary>
    public Dictionary<string, int> RoleIndexes { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Customer"] = 0,
        ["Tray"] = 1,
        ["ItemInsert"] = 2,
        ["ItemReturn"] = 3,
        ["LiveAgent"] = 4,
    };

    /// <summary>Warm-up wait after selecting a camera before snapping (legacy <c>WaitBeforeCaptureImage</c>).</summary>
    [Range(0, 30_000)]
    public int WarmupDelayMs { get; set; } = 2000;
}
