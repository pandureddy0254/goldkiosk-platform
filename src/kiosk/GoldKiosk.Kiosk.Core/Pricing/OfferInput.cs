namespace GoldKiosk.Kiosk.Core.Pricing;

/// <summary>
/// The pricing inputs for one offer calculation.
/// </summary>
/// <param name="Kind">The offer kind: <c>sale</c> or <c>pawn</c>.</param>
/// <param name="WeightGrams">The item weight in grams.</param>
/// <param name="GoldPercent">The measured gold content in percent (e.g. 91.6 for 22K).</param>
public sealed record OfferInput(string Kind, decimal WeightGrams, decimal GoldPercent);
