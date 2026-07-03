using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Rates;
using GoldKiosk.Domain.ValueObjects;
using GoldKiosk.Kiosk.Core.Options;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Kiosk.Api.Endpoints;

/// <summary>
/// The display-safe attract-loop market ticker (payload samples §13), served from the
/// configured mock rate table until live cloud pricing arrives.
/// </summary>
public static class RatesEndpoints
{
    /// <summary>Maps the rates endpoint onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapRatesEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/rates", GetRates);
        return group;
    }

    private static Ok<RatesResponse> GetRates(
        IOptions<MockRatesOptions> ratesOptions,
        TimeProvider timeProvider)
    {
        MockRatesOptions rates = ratesOptions.Value;
        return TypedResults.Ok(new RatesResponse(
            timeProvider.GetUtcNow(),
            [
                new MetalRateDto("gold", ToMoneyDto(rates.GoldPerGram, rates.Currency), rates.GoldChangePercent),
                new MetalRateDto("silver", ToMoneyDto(rates.SilverPerGram, rates.Currency), rates.SilverChangePercent),
            ]));
    }

    private static MoneyDto ToMoneyDto(decimal amount, string currency)
    {
        var money = Money.From(amount, currency);
        return new MoneyDto(money.ToMinorUnits(), money.CurrencyCode, money.ToString());
    }
}
