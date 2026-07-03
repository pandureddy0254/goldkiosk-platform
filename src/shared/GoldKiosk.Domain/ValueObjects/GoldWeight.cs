using System.Globalization;

namespace GoldKiosk.Domain.ValueObjects;

/// <summary>
/// A non-negative weight of precious metal in grams, with troy pennyweight conversion.
/// </summary>
public sealed record GoldWeight
{
    /// <summary>Grams per troy pennyweight (dwt).</summary>
    public const decimal GramsPerPennyweight = 1.55517384m;

    private GoldWeight(decimal grams) => Grams = grams;

    /// <summary>Gets the weight in grams.</summary>
    public decimal Grams { get; }

    /// <summary>Creates a weight from grams; the value must be zero or positive.</summary>
    /// <param name="grams">The weight in grams.</param>
    /// <returns>The weight value.</returns>
    public static GoldWeight FromGrams(decimal grams)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(grams);
        return new GoldWeight(grams);
    }

    /// <summary>Converts the weight to troy pennyweight (dwt).</summary>
    /// <returns>The weight in pennyweight.</returns>
    public decimal ToDwt() => Grams / GramsPerPennyweight;

    /// <summary>Formats the weight invariantly, e.g. <c>12.34 g</c>.</summary>
    /// <returns>The display string.</returns>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Grams} g");
}
