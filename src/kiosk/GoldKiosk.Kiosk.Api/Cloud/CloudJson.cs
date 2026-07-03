using System.Text.Json;

namespace GoldKiosk.Kiosk.Api.Cloud;

/// <summary>
/// The single snake_case JSON contract shared by the cloud wire (Cloud.Api serializes
/// snake_case) and the on-disk <c>journal.json</c> (written snake_case by the edge store).
/// Case-insensitive on read so a schema tweak never breaks the outbox.
/// </summary>
internal static class CloudJson
{
    /// <summary>The shared serializer options for cloud requests/responses and journal reads.</summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };
}
