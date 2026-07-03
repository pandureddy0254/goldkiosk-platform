using System.Globalization;
using GoldKiosk.Cloud.Api.Logging;
using GoldKiosk.Cloud.Api.Options;
using GoldKiosk.Cloud.Api.Services.Rates;
using GoldKiosk.Contracts.V1.Cloud.Offers;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.Api.Services.Offers;

/// <summary>
/// Offer computation (adapted from platform2's <c>OfferService</c> with the reuse-map
/// fixes applied): rate per gram comes from the latest <c>pricing.metal_rates</c> row for
/// the requested karat/currency; the margin comes from configuration — the tenant-level
/// pricing entity overrides it when modeled (TODO GK-TEN-1) — and rounding is the Domain
/// house-favorable <see cref="Money.FloorToNearest"/> policy. No hardcoded fallback
/// rates: an empty rate table refuses the offer rather than fabricating a price.
/// </summary>
/// <param name="rateReader">The last-known rate reader.</param>
/// <param name="quoteStore">The quote store grounding <c>offers/explain</c>.</param>
/// <param name="offersOptions">The offer policy (margin, rounding step, lock).</param>
/// <param name="regionOptions">The deployment region (currency).</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The host logger.</param>
public sealed class OfferService(
    IMetalRateReader rateReader,
    OfferQuoteStore quoteStore,
    IOptions<OffersOptions> offersOptions,
    IOptions<RegionOptions> regionOptions,
    TimeProvider timeProvider,
    ILogger<OfferService> logger) : IOfferService
{
    private const decimal FineKarat = 24m;

    /// <inheritdoc />
    public async Task<CloudOfferResponse?> ComputeOfferAsync(
        CloudOfferRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string currency = regionOptions.Value.DefaultCurrency;
        string metal = request.Metal.ToLowerInvariant();

        RateRow? rate = await ResolveRateAsync(metal, request.Karat, currency, cancellationToken);
        if (rate is null)
        {
            logger.OfferRatesMissing(metal, request.Karat, currency);
            return null;
        }

        OffersOptions policy = offersOptions.Value;
        decimal ratePerGram = EffectiveRatePerGram(rate, metal, request.Karat);
        decimal meltAmount = request.WeightGrams * (request.PurityPercent / 100m) * ratePerGram;

        Money melt = Money.From(meltAmount, currency);
        Money offer = Money
            .From(meltAmount * (policy.DefaultMarginPercent / 100m), currency)
            .FloorToNearest(policy.RoundingStep);

        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset lockedUntil = now.AddSeconds(policy.LockSeconds);
        string summary = string.Create(
            CultureInfo.InvariantCulture,
            $"1 Pc {request.Karat:0.#}K {metal} {request.Category} · {request.WeightGrams:F2} g");

        var quote = new OfferQuote(
            OfferId: Guid.NewGuid(),
            Amount: offer,
            MeltValue: melt,
            Summary: summary,
            Metal: metal,
            Karat: request.Karat,
            WeightGrams: request.WeightGrams,
            MarginPercent: policy.DefaultMarginPercent,
            RatePerGram: Money.From(ratePerGram, currency),
            RateAsOf: rate.RetrievedAt,
            LockedUntil: lockedUntil);
        quoteStore.Add(quote);

        string display = offer.ToString();
        logger.OfferComputed(quote.OfferId, display, summary);
        return new CloudOfferResponse(
            quote.OfferId,
            new MoneyDto(offer.ToMinorUnits(), offer.CurrencyCode, display),
            summary,
            rate.RetrievedAt,
            lockedUntil);
    }

    private async Task<RateRow?> ResolveRateAsync(
        string metal, decimal karat, string currency, CancellationToken cancellationToken)
    {
        if (metal == "silver")
        {
            return await rateReader.GetLatestAsync("silver", FineKarat, currency, cancellationToken);
        }

        // Exact karat row when the feed publishes it (24/22/18), otherwise scale fine gold.
        RateRow? exact = await rateReader.GetLatestAsync(metal, karat, currency, cancellationToken);
        return exact ?? await rateReader.GetLatestAsync(metal, FineKarat, currency, cancellationToken);
    }

    private static decimal EffectiveRatePerGram(RateRow rate, string metal, decimal karat)
    {
        if (metal == "silver" || rate.PurityKarat == karat)
        {
            return rate.PricePerGram;
        }

        // Fine-gold row scaled linearly by karat — purity-true, margin stays explicit
        // (the legacy karat factors baked margin into the rate; the new Domain separates them).
        return rate.PricePerGram * (karat / FineKarat);
    }
}
