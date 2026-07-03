using GoldKiosk.Domain.ValueObjects;

namespace GoldKiosk.Cloud.Api.Services.Offers;

/// <summary>
/// A computed offer quote held server-side for the lock window. Grounds the
/// <c>offers/explain</c> endpoint and the eventual transaction record.
/// </summary>
/// <param name="OfferId">The quote id.</param>
/// <param name="Amount">The offered amount (floored to the rounding step).</param>
/// <param name="MeltValue">The raw melt value before margin.</param>
/// <param name="Summary">The one-line item summary.</param>
/// <param name="Metal">The assayed metal.</param>
/// <param name="Karat">The assayed karat.</param>
/// <param name="WeightGrams">The measured weight in grams.</param>
/// <param name="MarginPercent">The margin applied, in percent of melt.</param>
/// <param name="RatePerGram">The market rate per gram used.</param>
/// <param name="RateAsOf">When the market rate was sourced.</param>
/// <param name="LockedUntil">When the price lock expires.</param>
public sealed record OfferQuote(
    Guid OfferId,
    Money Amount,
    Money MeltValue,
    string Summary,
    string Metal,
    decimal Karat,
    decimal WeightGrams,
    decimal MarginPercent,
    Money RatePerGram,
    DateTimeOffset RateAsOf,
    DateTimeOffset LockedUntil);
