using GoldKiosk.Cloud.Api.Auth;
using GoldKiosk.Cloud.Api.Errors;
using GoldKiosk.Cloud.Api.Options;
using GoldKiosk.Cloud.Api.Services.Rates;
using GoldKiosk.Contracts.V1.Cloud.Rates;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Domain.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.Api.Endpoints;

/// <summary>
/// Metal rates for kiosks (replaces legacy <c>GET_RATES</c>): the last-known
/// <c>pricing.metal_rates</c> rows, kept fresh by the sync worker.
/// </summary>
public static class RatesEndpoints
{
    /// <summary>Maps the rates endpoint onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapRatesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/rates", GetRatesAsync)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return group;
    }

    private static async Task<Results<Ok<CloudRatesResponse>, ProblemHttpResult>> GetRatesAsync(
        IMetalRateReader rateReader,
        IOptions<RegionOptions> regionOptions,
        CancellationToken cancellationToken)
    {
        string currency = regionOptions.Value.DefaultCurrency;
        IReadOnlyList<RateRow> rows = await rateReader.GetLatestPerMetalAsync(currency, cancellationToken);
        if (rows.Count == 0)
        {
            return Problems.RatesUnavailable();
        }

        List<CloudMetalRateDto> rates = [.. rows.Select(ToDto)];
        return TypedResults.Ok(new CloudRatesResponse(
            rows.Max(r => r.RetrievedAt), currency, rates));
    }

    private static CloudMetalRateDto ToDto(RateRow row)
    {
        var perGram = Money.From(row.PricePerGram, row.Currency);
        return new CloudMetalRateDto(
            row.Metal,
            row.PurityKarat,
            new MoneyDto(perGram.ToMinorUnits(), perGram.CurrencyCode, perGram.ToString()),
            row.RetrievedAt);
    }
}
