using GoldKiosk.Cloud.AdminPortal.Logging;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>
/// Background loop that refreshes the JWKS + revocation list at the configured
/// cadence. Runs the first refresh immediately on startup so a freshly started
/// dashboard doesn't reject the first activation attempt because JWKS hasn't
/// been fetched yet.
/// </summary>
public sealed class LicenseRefreshHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptions<LicensingOptions> _options;
    private readonly ILogger<LicenseRefreshHostedService> _logger;

    /// <summary>Initializes a new instance of the <see cref="LicenseRefreshHostedService"/> class.</summary>
    public LicenseRefreshHostedService(
        IServiceScopeFactory scopes,
        IOptions<LicensingOptions> options,
        ILogger<LicenseRefreshHostedService> logger)
    {
        _scopes = scopes;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshOnceAsync(stoppingToken);

        var jwksPeriod = TimeSpan.FromMinutes(Math.Max(1, _options.Value.JwksRefreshMinutes));
        var revPeriod = TimeSpan.FromMinutes(Math.Max(1, _options.Value.RevocationRefreshMinutes));

        var nextJwks = DateTimeOffset.UtcNow + jwksPeriod;
        var nextRev = DateTimeOffset.UtcNow + revPeriod;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException) { return; }

            var now = DateTimeOffset.UtcNow;
            using var scope = _scopes.CreateScope();
            if (now >= nextJwks)
            {
                await scope.ServiceProvider.GetRequiredService<JwksClient>().RefreshAsync(stoppingToken);
                nextJwks = now + jwksPeriod;
            }
            if (now >= nextRev)
            {
                await scope.ServiceProvider.GetRequiredService<RevocationClient>().RefreshAsync(stoppingToken);
                nextRev = now + revPeriod;
            }
        }
    }

    private async Task RefreshOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            await scope.ServiceProvider.GetRequiredService<JwksClient>().RefreshAsync(ct);
            await scope.ServiceProvider.GetRequiredService<RevocationClient>().RefreshAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.InitialLicenseRefreshFailed(ex);
        }
    }
}
