using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.Api.Options;

/// <summary>
/// GoldAPI.io live-rate feed settings. The API key is a secret: dev via
/// <c>dotnet user-secrets set "GoldApi:ApiKey" …</c>, prod via Key Vault. An empty key
/// disables live fetching and the platform degrades to last-known DB rates.
/// </summary>
public sealed class GoldApiOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "GoldApi";

    /// <summary>Gets the GoldAPI.io access token. Secret — from user-secrets/Key Vault; empty = no live fetch.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Gets the API base URL.</summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string BaseUrl { get; init; } = "https://www.goldapi.io/api";

    /// <summary>Gets the sync interval in hours (rates are refreshed once per interval).</summary>
    [Range(1, 168)]
    public int IntervalHours { get; init; } = 24;

    /// <summary>Gets a value indicating whether the background rate sync runs in this host.</summary>
    public bool EnableSync { get; init; } = true;
}
