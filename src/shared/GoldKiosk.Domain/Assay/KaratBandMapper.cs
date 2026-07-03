namespace GoldKiosk.Domain.Assay;

/// <summary>
/// Maps a karat value to the legacy UI band label, ported verbatim from
/// <c>GCGoldKaratMapper</c>. The labels are the legacy asset keys (with the <c>ktg</c> suffix).
/// </summary>
/// <remarks>
/// The legacy mapper has no distinct <c>gold-24ktg</c> band: the top band <see cref="Gold22"/>
/// covers 22–24 karat inclusive. Values outside 0–24 map to <see cref="None"/>.
/// </remarks>
public static class KaratBandMapper
{
    /// <summary>Band label for 0 &lt;= karat &lt; 8.</summary>
    public const string Gold0 = "gold-0ktg";

    /// <summary>Band label for 8 &lt;= karat &lt; 10.</summary>
    public const string Gold8To9 = "gold-8-9ktg";

    /// <summary>Band label for 10 &lt;= karat &lt; 14.</summary>
    public const string Gold10 = "gold-10ktg";

    /// <summary>Band label for 14 &lt;= karat &lt; 18.</summary>
    public const string Gold14 = "gold-14ktg";

    /// <summary>Band label for 18 &lt;= karat &lt; 22.</summary>
    public const string Gold18 = "gold-18ktg";

    /// <summary>Band label for 22 &lt;= karat &lt;= 24.</summary>
    public const string Gold22 = "gold-22ktg";

    /// <summary>The empty band label for karat values outside 0–24.</summary>
    public const string None = "";

    private static readonly int[] NominalKarats = [8, 10, 14, 18, 22, 24];

    /// <summary>Maps a karat value to its band label.</summary>
    /// <param name="karat">The karat value.</param>
    /// <returns>The band label, or <see cref="None"/> when the karat is outside 0–24.</returns>
    public static string Map(decimal karat) => karat switch
    {
        >= 0m and < 8m => Gold0,
        >= 8m and < 10m => Gold8To9,
        >= 10m and < 14m => Gold10,
        >= 14m and < 18m => Gold14,
        >= 18m and < 22m => Gold18,
        >= 22m and <= 24m => Gold22,
        _ => None,
    };

    /// <summary>
    /// Maps a karat value to its band label after snapping it to the nearest nominal karat stamp
    /// within <paramref name="rangePercentage"/> (legacy <c>KaratRange</c> tolerance). A
    /// <paramref name="rangePercentage"/> of 0 maps the karat directly. Higher nominal stamps take
    /// priority when tolerance bands overlap, matching the legacy reversed-list evaluation.
    /// </summary>
    /// <param name="karat">The karat value.</param>
    /// <param name="rangePercentage">The tolerance percentage (0–100); must not be negative.</param>
    /// <returns>The band label for the snapped karat.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="rangePercentage"/> is negative.</exception>
    public static string Map(decimal karat, decimal rangePercentage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rangePercentage);
        if (rangePercentage == 0m)
        {
            return Map(karat);
        }

        var multiplier = rangePercentage / 100m;
        for (var i = NominalKarats.Length - 1; i >= 0; i--)
        {
            decimal nominal = NominalKarats[i];
            var lower = nominal - nominal * multiplier;
            var upper = nominal + nominal * multiplier;
            if (karat >= lower && karat <= upper)
            {
                return Map(nominal);
            }
        }

        return Map(karat);
    }
}
