namespace GoldKiosk.Domain.ValueObjects;

/// <summary>
/// Precious-metal purity, expressed in karat with fineness (parts-per-thousand) conversion.
/// </summary>
/// <remarks>
/// Karat is the canonical representation so both factories round-trip exactly:
/// <c>FromKarat(18).Fineness == 750</c> and <c>FromFineness(916.7m).Fineness == 916.7</c>.
/// The fineness lower bound of 333 is the nominal 8-karat stamp; its exact karat
/// conversion (7.992) is preserved rather than rounded up.
/// </remarks>
public sealed record Purity
{
    /// <summary>The lowest accepted purity in karat.</summary>
    public const decimal MinKarat = 8m;

    /// <summary>The highest possible purity in karat (pure metal).</summary>
    public const decimal MaxKarat = 24m;

    /// <summary>The lowest accepted fineness in parts per thousand (nominal 8 karat).</summary>
    public const decimal MinFineness = 333m;

    /// <summary>The highest possible fineness in parts per thousand (pure metal).</summary>
    public const decimal MaxFineness = 1000m;

    private Purity(decimal karat) => Karat = karat;

    /// <summary>Gets the purity in karat (24 = pure metal).</summary>
    public decimal Karat { get; }

    /// <summary>Gets the purity as fineness in parts per thousand (e.g. 750 for 18 karat).</summary>
    public decimal Fineness => Karat * 1000m / 24m;

    /// <summary>Creates a purity from a karat value between 8 and 24 inclusive.</summary>
    /// <param name="karat">The purity in karat.</param>
    /// <returns>The purity value.</returns>
    public static Purity FromKarat(decimal karat)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(karat, MinKarat);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(karat, MaxKarat);
        return new Purity(karat);
    }

    /// <summary>Creates a purity from a fineness value between 333 and 1000 inclusive.</summary>
    /// <param name="fineness">The purity as fineness in parts per thousand.</param>
    /// <returns>The purity value.</returns>
    public static Purity FromFineness(decimal fineness)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(fineness, MinFineness);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fineness, MaxFineness);
        return new Purity(fineness * 24m / 1000m);
    }
}
