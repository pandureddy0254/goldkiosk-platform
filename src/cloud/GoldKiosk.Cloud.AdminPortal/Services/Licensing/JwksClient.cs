using GoldKiosk.Cloud.AdminPortal.Logging;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>
/// Fetches and caches the CRM's JWKS document. Holds the most recent
/// <see cref="LicenseVerifier"/> built from the merged JWKS (pinned + live).
/// Thread-safe — the <see cref="Verifier"/> reference is replaced atomically
/// after each successful refresh.
/// </summary>
public sealed class JwksClient
{
    private readonly HttpClient _http;
    private readonly IOptions<LicensingOptions> _options;
    private readonly ILogger<JwksClient> _logger;

    private volatile LicenseVerifier _verifier;
    private DateTimeOffset _lastRefreshed = DateTimeOffset.MinValue;

    /// <summary>Initializes a new instance of the <see cref="JwksClient"/> class.</summary>
    public JwksClient(HttpClient http, IOptions<LicensingOptions> options, ILogger<JwksClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
        _verifier = BuildPinnedOnly();
    }

    /// <summary>Gets the verifier.</summary>
    public LicenseVerifier Verifier => _verifier;
    /// <summary>Gets the last refreshed.</summary>
    public DateTimeOffset LastRefreshed => _lastRefreshed;

    /// <summary>Refresh.</summary>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var opt = _options.Value;

        if (opt.AirGapped)
        {
            _verifier = BuildPinnedOnly();
            _lastRefreshed = DateTimeOffset.UtcNow;
            return;
        }

        try
        {
            var url = opt.CrmBaseUrl.TrimEnd('/') + "/.well-known/jwks.json";
            var json = await _http.GetStringAsync(url, ct);
            _verifier = MergePinnedWithLive(opt.PinnedJwks, json);
            _lastRefreshed = DateTimeOffset.UtcNow;
            _logger.JwksRefreshed(url);
        }
        catch (Exception ex)
        {
            _logger.JwksRefreshFailed(ex);
        }
    }

    private LicenseVerifier BuildPinnedOnly()
    {
        var pinned = _options.Value.PinnedJwks;
        if (string.IsNullOrWhiteSpace(pinned))
        {
            return new LicenseVerifier(new Dictionary<string, byte[]>());
        }

        try
        {
            return LicenseVerifier.FromJwks(pinned);
        }
        catch (Exception ex)
        {
            _logger.PinnedJwksMalformed(ex);
            return new LicenseVerifier(new Dictionary<string, byte[]>());
        }
    }

    private static LicenseVerifier MergePinnedWithLive(string? pinned, string live)
    {
        // Live takes precedence so a rotated key is picked up immediately.
        var merged = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(pinned))
        {
            foreach (var (k, v) in LicenseVerifier.FromJwks(pinned).PublicKeysByKid)
            {
                merged[k] = v;
            }
        }

        foreach (var (k, v) in LicenseVerifier.FromJwks(live).PublicKeysByKid)
        {
            merged[k] = v;
        }

        return new LicenseVerifier(merged);
    }
}
