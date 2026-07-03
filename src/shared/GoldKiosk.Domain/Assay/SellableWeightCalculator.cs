using GoldKiosk.Domain.ValueObjects;

namespace GoldKiosk.Domain.Assay;

/// <summary>
/// Computes the sellable (payable) metal weight after subtracting the estimated stone mass from
/// the scale weight, clamped to the physically sensible range.
/// </summary>
public static class SellableWeightCalculator
{
    /// <summary>
    /// Computes <c>sellable = max(0, min(scaleWeight, scaleWeight − stoneTotalGrams))</c> so the
    /// sellable weight is never negative and never exceeds the scale weight (even if the stone
    /// estimate is negative or overshoots the scale weight).
    /// </summary>
    /// <param name="scaleWeight">The gross weight measured on the scale.</param>
    /// <param name="stoneTotalGrams">The estimated total stone mass in grams.</param>
    /// <returns>The clamped sellable metal weight.</returns>
    public static GoldWeight Compute(GoldWeight scaleWeight, decimal stoneTotalGrams)
    {
        ArgumentNullException.ThrowIfNull(scaleWeight);

        var scale = scaleWeight.Grams;
        var sellable = Math.Max(0m, Math.Min(scale, scale - stoneTotalGrams));
        return GoldWeight.FromGrams(sellable);
    }
}
