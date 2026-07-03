namespace GoldKiosk.Domain.Assay;

/// <summary>
/// Pure karat/assay calculator ported verbatim from the legacy <c>GCKaratCalculator</c>.
/// Computes both the density-weighted and raw-XRF karat, the estimated volume, and the
/// silver mass from a set of XRF element readings and the item's weight.
/// </summary>
/// <remarks>
/// The legacy code used <see cref="double"/>; this port uses <see cref="decimal"/> to satisfy
/// the money/precision standard while preserving the exact formulas and rounding (4 decimal
/// places, banker's rounding — matching <c>Math.Round(x, 4)</c>).
/// </remarks>
public sealed class KaratCalculator
{
    private const string GoldSymbol = "Au";
    private const string SilverSymbol = "Ag";
    private const int VolumeAndKaratScale = 4;

    private readonly ElementMap _elementMap;

    /// <summary>Creates a calculator backed by the given element density map.</summary>
    /// <param name="elementMap">The element density table (see <see cref="EmbeddedElementMap.Default"/>).</param>
    public KaratCalculator(ElementMap elementMap)
    {
        ArgumentNullException.ThrowIfNull(elementMap);
        _elementMap = elementMap;
    }

    /// <summary>Gets a shared calculator backed by the embedded default element density table.</summary>
    public static KaratCalculator Default { get; } = new(EmbeddedElementMap.Default);

    /// <summary>
    /// Computes the karat result for a set of XRF element readings and the item's weight.
    /// </summary>
    /// <param name="readings">The XRF element readings; must contain at least one element.</param>
    /// <param name="weightGrams">The item's weight in grams; must not be negative.</param>
    /// <returns>The computed <see cref="KaratResult"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="readings"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="weightGrams"/> is negative.</exception>
    /// <exception cref="KeyNotFoundException">A reading references an element that is not in the density map.</exception>
    /// <exception cref="InvalidOperationException">The element percentages sum to a non-positive density ratio.</exception>
    public KaratResult Calculate(IReadOnlyList<ElementReading> readings, decimal weightGrams)
    {
        ArgumentNullException.ThrowIfNull(readings);
        ArgumentOutOfRangeException.ThrowIfNegative(weightGrams);
        if (readings.Count == 0)
        {
            throw new ArgumentException("At least one element reading is required.", nameof(readings));
        }

        decimal totalDensityRatio = 0m;
        foreach (var reading in readings)
        {
            totalDensityRatio += _elementMap.GetDensity(reading.Symbol) * reading.Percent / 100m;
        }

        if (totalDensityRatio <= 0m)
        {
            throw new InvalidOperationException(
                "The element percentages sum to a non-positive density ratio; the analysis payload is invalid.");
        }

        var goldPercent = PercentOf(readings, GoldSymbol);
        var silverPercent = PercentOf(readings, SilverSymbol);

        var goldRatio = goldPercent * _elementMap.GetDensity(GoldSymbol) / 100m;
        var goldWeightPercent = goldRatio / totalDensityRatio * 100m;

        var calculatedVolume = decimal.Round(weightGrams / totalDensityRatio, VolumeAndKaratScale);
        var karatUsingDensity = decimal.Round(goldWeightPercent * 24m / 100m, VolumeAndKaratScale);
        var karatUsingXrfPercentage = decimal.Round(goldPercent * 24m / 100m, VolumeAndKaratScale);

        var silverRatio = silverPercent * _elementMap.GetDensity(SilverSymbol) / 100m;
        var silverWeightGrams = silverRatio / totalDensityRatio * weightGrams;

        return new KaratResult(
            karatUsingDensity,
            karatUsingXrfPercentage,
            calculatedVolume,
            goldWeightPercent,
            silverWeightGrams);
    }

    private static decimal PercentOf(IReadOnlyList<ElementReading> readings, string symbol)
    {
        foreach (var reading in readings)
        {
            if (string.Equals(reading.Symbol, symbol, StringComparison.OrdinalIgnoreCase))
            {
                return reading.Percent;
            }
        }

        return 0m;
    }
}
