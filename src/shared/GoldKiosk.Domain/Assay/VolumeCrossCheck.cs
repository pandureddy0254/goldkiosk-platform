namespace GoldKiosk.Domain.Assay;

/// <summary>
/// Pure volume fraud cross-check ported from the legacy <c>GCVolumeRangeValidator</c>,
/// <c>GCKaratCalculator.GetKaratAfterVolume</c>, and the 1–10 g weight-adjustment path in
/// <c>MetalAnalyserCommandExecutorNew</c>. Compares the calculated volume (from the karat math)
/// with the physically measured volume and recommends a correction or rejection.
/// </summary>
public static class VolumeCrossCheck
{
    /// <summary>
    /// Evaluates the volume cross-check.
    /// </summary>
    /// <param name="calculatedVolume">The calculated volume (cm³) from <see cref="KaratResult.CalculatedVolume"/>.</param>
    /// <param name="measuredVolume">The physically measured volume (cm³); must be positive.</param>
    /// <param name="weightGrams">The item's weight in grams; must not be negative.</param>
    /// <param name="options">The assay thresholds.</param>
    /// <returns>The volume cross-check result.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="measuredVolume"/> is not positive, or <paramref name="weightGrams"/> is negative.</exception>
    public static VolumeCrossCheckResult Evaluate(
        decimal calculatedVolume,
        decimal measuredVolume,
        decimal weightGrams,
        AssayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(measuredVolume);
        ArgumentOutOfRangeException.ThrowIfNegative(weightGrams);

        var correctionFactor = calculatedVolume / measuredVolume * 100m;
        var ratio = correctionFactor / 100m;
        var isInRange = correctionFactor >= options.VolumeInRangeMinPercent;

        var correction = VolumeCorrection.None;
        var elementScaleFactor = 1m;
        var adjustedWeightGrams = weightGrams;
        RejectionReason? rejection = null;

        // The fraud cross-check only runs when the measured volume exceeds the calculated volume
        // and the calculated volume clears the minimum threshold (legacy FraudDetection gate).
        var fraudApplicable =
            measuredVolume > calculatedVolume &&
            calculatedVolume > options.CalculatedVolumeThresholdLimit;

        if (fraudApplicable)
        {
            if (weightGrams > options.HeavyItemWeightGrams &&
                correctionFactor >= options.VolumeInRangeMinPercent &&
                correctionFactor <= options.HeavyScalingMaxPercent)
            {
                // Heavy item: reduce the karat by scaling the element percentages by the ratio.
                correction = VolumeCorrection.HeavyElementScaling;
                elementScaleFactor = ratio;
            }
            else if (weightGrams > options.VolumeRejectWeightGrams &&
                     weightGrams <= options.HeavyItemWeightGrams &&
                     ratio >= options.WeightAdjustLowerRatio &&
                     ratio <= options.WeightAdjustUpperRatio &&
                     ratio < options.WeightAdjustmentRatioThreshold)
            {
                // Light item (1–10 g): reduce the paid weight by the ratio.
                correction = VolumeCorrection.WeightAdjustment;
                adjustedWeightGrams = ratio * weightGrams;
            }

            if (weightGrams > options.VolumeRejectWeightGrams && !isInRange)
            {
                rejection = RejectionReason.UnacceptableVolumeError;
            }
        }

        return new VolumeCrossCheckResult(
            correctionFactor,
            ratio,
            isInRange,
            correction,
            elementScaleFactor,
            adjustedWeightGrams,
            rejection);
    }
}
