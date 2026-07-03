using System.Text.Json;

namespace GoldKiosk.TestKit;

/// <summary>
/// The snake_case wire serializer options matching the Kiosk.Api host
/// (<c>ConfigureHttpJsonOptions</c> in Program.cs): web defaults + snake_case properties
/// and dictionary keys.
/// </summary>
public static class WireJson
{
    /// <summary>Serializer options equivalent to the Kiosk.Api wire configuration.</summary>
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}
