namespace GoldKiosk.Domain.Assay;

/// <summary>
/// The result of the karat/assay computation for a single item, ported verbatim from the
/// legacy <c>GCKaratCalculator</c>. All values are derived deterministically from the XRF
/// element readings and the item's weight; no rejection decisions are made here (see
/// <see cref="AssayRejectionPolicy"/> and <see cref="VolumeCrossCheck"/>).
/// </summary>
/// <param name="KaratUsingDensity">
/// The karat derived from the density-weighted gold fraction:
/// <c>round(goldWeightPercent · 24 / 100, 4)</c>.
/// </param>
/// <param name="KaratUsingXrfPercentage">
/// The karat derived directly from the raw XRF gold percentage: <c>round(Au% · 24 / 100, 4)</c>.
/// This is the value used for the offer when <see cref="AssayOptions.UseKaratUsingXrfPercentageForOffer"/> is set.
/// </param>
/// <param name="CalculatedVolume">
/// The estimated item volume in cm³: <c>round(weightGrams / totalDensityRatio, 4)</c>,
/// used by the volume fraud cross-check.
/// </param>
/// <param name="GoldWeightPercent">
/// The density-weighted gold fraction as a percentage (unrounded):
/// <c>goldRatio / totalDensityRatio · 100</c>.
/// </param>
/// <param name="SilverWeightGrams">
/// The estimated mass of silver in grams (legacy <c>SilverWeight</c>):
/// <c>silverRatio / totalDensityRatio · weightGrams</c>. Zero when no silver is present.
/// </param>
public sealed record KaratResult(
    decimal KaratUsingDensity,
    decimal KaratUsingXrfPercentage,
    decimal CalculatedVolume,
    decimal GoldWeightPercent,
    decimal SilverWeightGrams);
