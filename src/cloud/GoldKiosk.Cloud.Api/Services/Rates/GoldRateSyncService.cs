using GoldKiosk.Cloud.Api.Logging;
using GoldKiosk.Cloud.Api.Options;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Pricing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.Api.Services.Rates;

/// <summary>
/// Background worker syncing market rates into <c>pricing.metal_rates</c> once per
/// <c>GoldApi:IntervalHours</c> (default 24 h). Adapted verbatim from platform2's
/// <c>GoldRateSyncService</c>: on startup it checks whether existing rates are still
/// fresh and waits out the remainder; after each sleep it re-checks so multiple API
/// instances against one database do not double-fetch; a failed fetch backs off and
/// keeps the last-known DB rates.
/// </summary>
/// <param name="scopeFactory">Creates DI scopes for the pooled <see cref="AppDbContext"/>.</param>
/// <param name="provider">The market-feed provider.</param>
/// <param name="goldApiOptions">The feed settings.</param>
/// <param name="regionOptions">The deployment region (currency).</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The host logger.</param>
public sealed class GoldRateSyncService(
    IServiceScopeFactory scopeFactory,
    IMetalPriceProvider provider,
    IOptions<GoldApiOptions> goldApiOptions,
    IOptions<RegionOptions> regionOptions,
    TimeProvider timeProvider,
    ILogger<GoldRateSyncService> logger) : BackgroundService
{
    private const string SourceCode = "goldapi.io";
    private static readonly TimeSpan FetchFailureBackoff = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan ErrorRetryDelay = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!goldApiOptions.Value.EnableSync)
        {
            logger.RateSyncDisabled();
            return;
        }

        logger.RateSyncStarted(goldApiOptions.Value.IntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                TimeSpan delay = await GetDelayUntilNextSyncAsync(stoppingToken);
                if (delay > TimeSpan.Zero)
                {
                    logger.RateSyncSleeping((int)delay.TotalMinutes);
                    await Task.Delay(delay, timeProvider, stoppingToken);
                }

                // Re-check after the delay — another instance may have synced while this
                // one was sleeping (several hosts can share one database).
                TimeSpan delayAfterWake = await GetDelayUntilNextSyncAsync(stoppingToken);
                if (delayAfterWake > TimeSpan.Zero)
                {
                    logger.RateSyncAlreadyFresh();
                    continue;
                }

                await SyncAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // The worker must survive transient DB/feed faults; the error is logged
                // and the loop retries after a fixed delay.
                logger.RateSyncError(ex, (int)ErrorRetryDelay.TotalMinutes);
                await Task.Delay(ErrorRetryDelay, timeProvider, stoppingToken);
            }
        }
    }

    private async Task<TimeSpan> GetDelayUntilNextSyncAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        DateTimeOffset? latest = await db.MetalRates
            .AsNoTracking()
            .OrderByDescending(r => r.RetrievedAt)
            .Select(r => (DateTimeOffset?)r.RetrievedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest is null)
        {
            return TimeSpan.Zero;
        }

        DateTimeOffset nextSync = latest.Value.AddHours(goldApiOptions.Value.IntervalHours);
        TimeSpan remaining = nextSync - timeProvider.GetUtcNow();
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private async Task SyncAsync(CancellationToken cancellationToken)
    {
        SpotRates? rates = await provider.FetchAsync(cancellationToken);
        if (rates is null)
        {
            logger.RateFetchReturnedNull();
            await Task.Delay(FetchFailureBackoff, timeProvider, cancellationToken);
            return;
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset validUntil = now.AddHours(goldApiOptions.Value.IntervalHours + 1); // +1h grace

        Guid sourceId = await EnsureSourceAsync(db, cancellationToken);
        string currency = regionOptions.Value.DefaultCurrency;

        MetalRate[] rows =
        [
            NewRate("gold", 24, rates.Gold24kPerGram, currency, sourceId, now, validUntil),
            NewRate("gold", 22, rates.Gold22kPerGram, currency, sourceId, now, validUntil),
            NewRate("gold", 18, rates.Gold18kPerGram, currency, sourceId, now, validUntil),
            NewRate("silver", 24, rates.SilverPerGram, currency, sourceId, now, validUntil),
        ];

        db.MetalRates.AddRange(rows);
        await db.SaveChangesAsync(cancellationToken);

        logger.RateSyncSaved(rows.Length, validUntil);
    }

    private static MetalRate NewRate(
        string metal,
        decimal karat,
        decimal pricePerGram,
        string currency,
        Guid sourceId,
        DateTimeOffset retrievedAt,
        DateTimeOffset validUntil) => new()
        {
            Id = Guid.NewGuid(),
            Metal = metal,
            PurityKarat = karat,
            PricePerGram = pricePerGram,
            CurrencyCode = currency,
            MetalRateSourceId = sourceId,
            RetrievedAt = retrievedAt,
            ValidUntil = validUntil,
        };

    private static async Task<Guid> EnsureSourceAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        MetalRateSource? source = await db.MetalRateSources
            .FirstOrDefaultAsync(s => s.Code == SourceCode, cancellationToken);

        if (source is null)
        {
            source = new MetalRateSource
            {
                Id = Guid.NewGuid(),
                Code = SourceCode,
                Name = "GoldAPI.io",
                AdapterType = "http",
                PollIntervalSeconds = 86400,  // 24h default
                FreshnessSlaSeconds = 90000,  // 25h SLA
                IsActive = true,
            };
            db.MetalRateSources.Add(source);
            await db.SaveChangesAsync(cancellationToken);
        }

        return source.Id;
    }
}
