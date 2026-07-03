namespace GoldKiosk.Domain.Assay;

/// <summary>
/// The outcome of the volume fraud cross-check: the correction factor, whether the volume is in
/// range, which correction (if any) to apply, and any rejection.
/// </summary>
/// <param name="CorrectionFactor">
/// The correction factor as a percentage: <c>calculatedVolume / measuredVolume · 100</c>.
/// </param>
/// <param name="Ratio">The correction factor as a ratio (<c>CorrectionFactor / 100</c>).</param>
/// <param name="IsInRange">
/// Whether the volume is in range (<c>CorrectionFactor &gt;= </c><see cref="AssayOptions.VolumeInRangeMinPercent"/>).
/// </param>
/// <param name="Correction">The recommended correction (see <see cref="VolumeCorrection"/>).</param>
/// <param name="ElementScaleFactor">
/// The factor to multiply each XRF element percentage by when
/// <see cref="Correction"/> is <see cref="VolumeCorrection.HeavyElementScaling"/>; otherwise 1.
/// </param>
/// <param name="AdjustedWeightGrams">
/// The recommended paid weight in grams: the ratio-scaled weight when
/// <see cref="Correction"/> is <see cref="VolumeCorrection.WeightAdjustment"/>; otherwise the original weight.
/// </param>
/// <param name="Rejection">
/// <see cref="RejectionReason.UnacceptableVolumeError"/> when the item must be rejected; otherwise <see langword="null"/>.
/// </param>
public sealed record VolumeCrossCheckResult(
    decimal CorrectionFactor,
    decimal Ratio,
    bool IsInRange,
    VolumeCorrection Correction,
    decimal ElementScaleFactor,
    decimal AdjustedWeightGrams,
    RejectionReason? Rejection);
