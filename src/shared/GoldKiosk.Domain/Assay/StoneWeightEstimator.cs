namespace GoldKiosk.Domain.Assay;

/// <summary>
/// Pure, <b>report-only</b> stone-weight estimator (vision/karat rebuild design note §3). Estimates
/// the mass of each detected stone from its projected area using the calibration's power law:
/// <c>carats = Alpha · AreaMm2 ^ Beta</c>, then <c>grams = carats · </c><see cref="StoneCalibration.GramsPerCarat"/>.
/// </summary>
/// <remarks>
/// The power law needs a fractional exponent, so the intermediate carat computation uses
/// <see cref="double"/> (<see cref="Math.Pow(double, double)"/>); the result is materialised as
/// <see cref="decimal"/> grams. This is explicitly <b>not</b> a money path — the estimate is
/// advisory only and must never adjust the paid weight until the model is calibrated.
/// </remarks>
public static class StoneWeightEstimator
{
    private const int GramsScale = 4;

    /// <summary>Estimates the stone masses for the detected regions.</summary>
    /// <param name="regions">The detected stone regions; each area must not be negative.</param>
    /// <param name="calibration">The stone calibration to apply.</param>
    /// <param name="declaredType">
    /// The customer-declared stone type, if any. When supplied it must match
    /// <see cref="StoneCalibration.StoneType"/>; it is otherwise informational.
    /// </param>
    /// <returns>The report-only <see cref="StoneWeightEstimate"/>.</returns>
    /// <exception cref="ArgumentException"><paramref name="declaredType"/> does not match the calibration's stone type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A region area is negative.</exception>
    public static StoneWeightEstimate Estimate(
        IReadOnlyList<StoneRegion> regions,
        StoneCalibration calibration,
        StoneType? declaredType = null)
    {
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(calibration);

        if (declaredType is { } declared && declared != calibration.StoneType)
        {
            throw new ArgumentException(
                $"Calibration is for {calibration.StoneType} but the declared stone type is {declared}.",
                nameof(declaredType));
        }

        var perStone = new List<decimal>(regions.Count);
        var total = 0m;
        foreach (var region in regions)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(region.AreaMm2);

            var carats = calibration.Alpha * Math.Pow(region.AreaMm2, calibration.Beta);
            var grams = decimal.Round((decimal)carats * StoneCalibration.GramsPerCarat, GramsScale);
            perStone.Add(grams);
            total += grams;
        }

        return new StoneWeightEstimate(total, perStone, StoneConfidence.ReportOnly);
    }
}
