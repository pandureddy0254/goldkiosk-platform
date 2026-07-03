using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// An RFC 7807 ProblemDetails body returned by the Kiosk API. The stable <see cref="Type"/>
/// code (see <c>GoldKiosk.Contracts.V1.Common.ProblemTypes</c>) drives the localized error
/// catalogue; extension members (e.g. <c>available_methods</c>) are kept for recovery routing.
/// </summary>
public sealed record KioskProblem
{
    /// <summary>Gets the stable problem type URI, e.g. <c>https://goldkiosk.dev/problems/offer.expired</c>.</summary>
    public string? Type { get; init; }

    /// <summary>Gets the short human-readable summary.</summary>
    public string? Title { get; init; }

    /// <summary>Gets the HTTP status code.</summary>
    public int? Status { get; init; }

    /// <summary>Gets the human-readable detail for this occurrence.</summary>
    public string? Detail { get; init; }

    /// <summary>Gets the extension members passed through opaquely (e.g. <c>available_methods</c>).</summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; init; }

    /// <summary>Gets the problem code with the base URI stripped, e.g. <c>offer.expired</c>.</summary>
    public string Code
    {
        get
        {
            const string baseUri = "https://goldkiosk.dev/problems/";
            return Type switch
            {
                null or "" => "generic",
                _ when Type.StartsWith(baseUri, StringComparison.Ordinal) => Type[baseUri.Length..],
                _ => Type,
            };
        }
    }

    /// <summary>
    /// Reads a string-array extension member (e.g. <c>available_methods</c>), or an empty
    /// list when absent or malformed.
    /// </summary>
    /// <param name="name">The snake_case extension member name.</param>
    /// <returns>The string values, or an empty list.</returns>
    public IReadOnlyList<string> GetStringList(string name)
    {
        if (Extensions is null
            || !Extensions.TryGetValue(name, out JsonElement element)
            || element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<string> values = [];
        foreach (JsonElement item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { } value)
            {
                values.Add(value);
            }
        }

        return values;
    }
}
