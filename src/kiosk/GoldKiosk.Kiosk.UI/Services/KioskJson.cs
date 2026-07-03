using System.Text.Json;

namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// The single shared JSON configuration for every wire exchange with the Kiosk API
/// (REST and SignalR). The API serializes with <see cref="JsonNamingPolicy.SnakeCaseLower"/>;
/// the UI must mirror it exactly.
/// </summary>
public static class KioskJson
{
    /// <summary>Gets the shared serializer options (snake_case, case-insensitive reads).</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}
