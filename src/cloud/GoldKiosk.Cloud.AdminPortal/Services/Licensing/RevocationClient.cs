using GoldKiosk.Cloud.AdminPortal.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>
/// Fetches and caches the CRM's revocation list. Lookup by AIKI-XXXXXXXX prefix.
/// </summary>
public sealed class RevocationClient
{
    private readonly HttpClient _http;
    private readonly IOptions<LicensingOptions> _options;
    private readonly ILogger<RevocationClient> _logger;

    private volatile HashSet<string> _revokedPrefixes;
    private DateTimeOffset _lastRefreshed = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes the client with the pinned revocation list (if configured).
    /// </summary>
    /// <param name="http">Long-lived HttpClient from the factory.</param>
    /// <param name="options">Licensing options carrying URLs and pinned documents.</param>
    /// <param name="logger">Logger for refresh diagnostics.</param>
    public RevocationClient(HttpClient http, IOptions<LicensingOptions> options, ILogger<RevocationClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
        _revokedPrefixes = LoadPinned();
    }

    /// <summary>When the revocation list was last refreshed successfully.</summary>
    public DateTimeOffset LastRefreshed => _lastRefreshed;

    /// <summary>True when the token's revocation prefix is on the revocation list.</summary>
    /// <param name="token">The raw AIKI- token.</param>
    public bool IsRevoked(string token)
    {
        var prefix = LicenseVerifier.ExtractRevocationPrefix(token);
        return prefix is not null && _revokedPrefixes.Contains(prefix);
    }

    /// <summary>
    /// Refreshes the revocation list from the CRM (or re-loads the pinned list
    /// when air-gapped). Failures keep the last-known list.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var opt = _options.Value;

        if (opt.AirGapped)
        {
            _revokedPrefixes = LoadPinned();
            _lastRefreshed = DateTimeOffset.UtcNow;
            return;
        }

        try
        {
            var url = opt.CrmBaseUrl.TrimEnd('/') + "/api/license/revoked.json";
            var json = await _http.GetStringAsync(new Uri(url), ct);
            _revokedPrefixes = ParsePrefixes(json);
            _lastRefreshed = DateTimeOffset.UtcNow;
            _logger.RevocationListRefreshed(_revokedPrefixes.Count);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.RevocationListRefreshFailed(ex);
        }
    }

    private HashSet<string> LoadPinned()
    {
        var pinned = _options.Value.PinnedRevocationList;
        return string.IsNullOrWhiteSpace(pinned)
            ? new HashSet<string>(StringComparer.Ordinal)
            : ParsePrefixes(pinned);
    }

    private static HashSet<string> ParsePrefixes(string json)
    {
        var doc = JsonSerializer.Deserialize<RevocationDoc>(json);
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (doc?.Revoked is null)
        {
            return set;
        }

        foreach (var r in doc.Revoked)
        {
            if (!string.IsNullOrWhiteSpace(r.Prefix))
            {
                set.Add(r.Prefix);
            }
        }

        return set;
    }

    private sealed class RevocationDoc
    {
        [JsonPropertyName("generated_at")]
        public string? GeneratedAt { get; set; }

        [JsonPropertyName("revoked")]
        public List<RevocationEntry>? Revoked { get; set; }
    }

    private sealed class RevocationEntry
    {
        [JsonPropertyName("prefix")]
        public string? Prefix { get; set; }

        [JsonPropertyName("revoked_at")]
        public string? RevokedAt { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }
}
