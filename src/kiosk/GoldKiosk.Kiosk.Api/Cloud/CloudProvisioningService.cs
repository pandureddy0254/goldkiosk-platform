using System.Text.Json;
using GoldKiosk.Kiosk.Core.Cloud;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Kiosk.Api.Cloud;

/// <summary>
/// Pulls the kiosk's provisioning (tenant/kiosk identity, live/trading status, trading
/// window) from Cloud.Api at startup and re-polls on an interval, publishing the result to
/// <see cref="CurrentProvisioning"/>. Caches the last-good result to
/// <c>%ProgramData%</c> so the kiosk boots provisioned when the cloud is unreachable
/// (architecture-principles §4). Never blocks startup: when <c>Cloud:Enabled=false</c> it
/// records the disabled state and exits; when the cloud is down it falls back to cache and
/// surfaces an offline/unprovisioned state rather than throwing.
/// </summary>
public sealed class CloudProvisioningService : BackgroundService
{
    private const string CacheFileName = "provisioning.json";

    private readonly ICloudGateway _gateway;
    private readonly CurrentProvisioning _current;
    private readonly IOptions<CloudOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CloudProvisioningService> _logger;

    /// <summary>Initializes the service.</summary>
    /// <param name="gateway">The cloud gateway.</param>
    /// <param name="current">The provisioning holder to publish into.</param>
    /// <param name="options">The validated cloud options.</param>
    /// <param name="timeProvider">The clock.</param>
    /// <param name="logger">The host logger.</param>
    public CloudProvisioningService(
        ICloudGateway gateway,
        CurrentProvisioning current,
        IOptions<CloudOptions> options,
        TimeProvider timeProvider,
        ILogger<CloudProvisioningService> logger)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _current = current ?? throw new ArgumentNullException(nameof(current));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        CloudOptions options = _options.Value;
        if (!options.Enabled)
        {
            // Offline demo path: the kiosk runs on local mock pricing, unchanged.
            _current.Set(new ProvisioningSnapshot(
                ProvisioningState.Disabled, Provisioning: null, _timeProvider.GetUtcNow()));
            _logger.CloudDisabled();
            return;
        }

        await RefreshAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.PollIntervalSeconds), _timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RefreshAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Deliberate catch-all: one failed poll must never stop the provisioning loop.
                _logger.CloudError(ex, "provisioning-poll");
            }
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        KioskProvisioning? provisioning = await _gateway.GetProvisioningAsync(cancellationToken);
        DateTimeOffset now = _timeProvider.GetUtcNow();

        if (provisioning is not null)
        {
            ProvisioningState state = provisioning.IsLive
                ? ProvisioningState.Provisioned
                : ProvisioningState.OutOfService;
            _current.Set(new ProvisioningSnapshot(state, provisioning, now));
            _logger.ProvisioningApplied(provisioning.KioskId, provisioning.IsLive);
            await WriteCacheAsync(provisioning, cancellationToken);
            return;
        }

        // Cloud unreachable: keep any live snapshot we already hold, else fall back to disk.
        if (_current.Provisioning is KioskProvisioning known)
        {
            _current.Set(new ProvisioningSnapshot(ProvisioningState.Offline, known, now));
            return;
        }

        KioskProvisioning? cached = await ReadCacheAsync(cancellationToken);
        if (cached is not null)
        {
            _current.Set(new ProvisioningSnapshot(ProvisioningState.Offline, cached, now));
            _logger.ProvisioningFromCache(cached.KioskId);
        }
        else
        {
            _current.Set(new ProvisioningSnapshot(ProvisioningState.Unprovisioned, Provisioning: null, now));
            _logger.ProvisioningUnavailable();
        }
    }

    private async Task WriteCacheAsync(KioskProvisioning provisioning, CancellationToken cancellationToken)
    {
        try
        {
            string root = _options.Value.CacheRoot;
            Directory.CreateDirectory(root);
            string path = Path.Combine(root, CacheFileName);
            string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            string json = JsonSerializer.Serialize(provisioning, CloudJson.Options);
            await File.WriteAllTextAsync(tempPath, json, cancellationToken);
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.ProvisioningCacheIoFailed(ex);
        }
    }

    private async Task<KioskProvisioning?> ReadCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            string path = Path.Combine(_options.Value.CacheRoot, CacheFileName);
            if (!File.Exists(path))
            {
                return null;
            }

            string json = await File.ReadAllTextAsync(path, cancellationToken);
            return JsonSerializer.Deserialize<KioskProvisioning>(json, CloudJson.Options);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            _logger.ProvisioningCacheIoFailed(ex);
            return null;
        }
    }
}
