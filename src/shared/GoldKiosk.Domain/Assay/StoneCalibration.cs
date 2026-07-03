namespace GoldKiosk.Domain.Assay;

/// <summary>
/// The (report-only) power-law calibration that maps a stone's projected area to its mass for a
/// given <see cref="StoneType"/>: <c>carats = Alpha · AreaMm2 ^ Beta</c>, converted to grams via
/// <see cref="GramsPerCarat"/>.
/// </summary>
/// <remarks>
/// The default <see cref="DiamondEquivalent"/> constants are <b>UNVALIDATED placeholders</b> for
/// the report-only estimator (see the vision/karat rebuild design note §3). They must be replaced
/// by regressed values from a calibration reference set before any estimate can affect the paid weight.
/// </remarks>
/// <param name="StoneType">The stone type this calibration models.</param>
/// <param name="Alpha">The power-law coefficient (carats per <c>AreaMm2 ^ Beta</c>).</param>
/// <param name="Beta">The power-law exponent (≈1.5 for roughly isometric cuts).</param>
/// <param name="DensityGramsPerCc">
/// The stone's density in g/cm³ (reference metadata for the future volumetric cross-check; not
/// used by the power-law estimate).
/// </param>
public sealed record StoneCalibration(StoneType StoneType, double Alpha, double Beta, decimal DensityGramsPerCc)
{
    /// <summary>Grams per metric carat (a carat is exactly 0.2 g).</summary>
    public const decimal GramsPerCarat = 0.2m;

    /// <summary>
    /// The default, <b>UNVALIDATED</b> diamond-equivalent calibration used until a real calibration
    /// is supplied. Density 3.52 g/cm³; power law tuned so a ~6.5 mm round brilliant (~33 mm²)
    /// estimates ≈1 carat.
    /// </summary>
    public static StoneCalibration DiamondEquivalent { get; } =
        new(StoneType.DiamondEquivalent, 0.0052d, 1.5d, 3.52m);
}
