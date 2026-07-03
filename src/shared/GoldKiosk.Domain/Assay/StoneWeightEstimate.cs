namespace GoldKiosk.Domain.Assay;

/// <summary>
/// A (report-only) estimate of the total mass of set stones within a jewellery item.
/// </summary>
/// <param name="TotalGrams">The estimated total stone mass in grams (sum of <see cref="PerStoneGrams"/>).</param>
/// <param name="PerStoneGrams">The estimated mass in grams of each detected stone, in input order.</param>
/// <param name="Confidence">The confidence tier of the estimate (currently always report-only).</param>
public sealed record StoneWeightEstimate(
    decimal TotalGrams,
    IReadOnlyList<decimal> PerStoneGrams,
    StoneConfidence Confidence);
