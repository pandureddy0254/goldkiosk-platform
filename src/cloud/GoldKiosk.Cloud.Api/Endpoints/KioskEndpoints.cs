using GoldKiosk.Cloud.Api.Auth;
using GoldKiosk.Cloud.Api.Errors;
using GoldKiosk.Cloud.Api.Options;
using GoldKiosk.Contracts.V1.Cloud.Kiosk;
using GoldKiosk.Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.Api.Endpoints;

/// <summary>
/// Kiosk config pull + trading gate (replaces legacy <c>KARAT-RANGE-PERCENTAGE</c>,
/// <c>APP-STATUS</c> and the cassette query's config role). Status comes from the fleet
/// record; the karat window and feature switches come from deployment config until the
/// tenant config entity is modeled (TODO GK-TEN-1).
/// </summary>
public static class KioskEndpoints
{
    /// <summary>Maps the kiosk config endpoints onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapKioskEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/kiosk/config", GetConfigAsync)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/kiosk/status", GetStatusAsync)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<Results<Ok<KioskConfigResponse>, ProblemHttpResult>> GetConfigAsync(
        CurrentKioskService currentKiosk,
        AppDbContext db,
        IOptions<KioskConfigOptions> configOptions,
        IOptions<RegionOptions> regionOptions,
        CancellationToken cancellationToken)
    {
        Infrastructure.Entities.Kiosk.Kiosk? kiosk =
            await FindKioskAsync(currentKiosk, db, cancellationToken);
        if (kiosk is null)
        {
            return Problems.KioskNotFound();
        }

        KioskConfigOptions config = configOptions.Value;
        return TypedResults.Ok(new KioskConfigResponse(
            kiosk.Id,
            kiosk.Code,
            kiosk.Status,
            kiosk.IsMaintenance,
            new KaratRangeDto(config.MinKarat, config.MaxKarat),
            new KioskFeaturesDto(config.PawnEnabled, config.CryptoEnabled, config.PayoutMethods),
            regionOptions.Value.DefaultCurrency));
    }

    private static async Task<Results<Ok<KioskStatusResponse>, ProblemHttpResult>> GetStatusAsync(
        CurrentKioskService currentKiosk,
        AppDbContext db,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        Infrastructure.Entities.Kiosk.Kiosk? kiosk =
            await FindKioskAsync(currentKiosk, db, cancellationToken);
        if (kiosk is null)
        {
            return Problems.KioskNotFound();
        }

        bool canTrade = kiosk.IsActive
            && !kiosk.IsMaintenance
            && string.Equals(kiosk.Status, "live", StringComparison.OrdinalIgnoreCase);
        return TypedResults.Ok(new KioskStatusResponse(
            kiosk.Status, kiosk.IsMaintenance, canTrade, timeProvider.GetUtcNow()));
    }

    private static async Task<Infrastructure.Entities.Kiosk.Kiosk?> FindKioskAsync(
        CurrentKioskService currentKiosk, AppDbContext db, CancellationToken cancellationToken)
    {
        if (currentKiosk.KioskId is not Guid kioskId)
        {
            return null;
        }

        return await db.Kiosks
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == kioskId, cancellationToken);
    }
}
