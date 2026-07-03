namespace GoldKiosk.Domain.Assay;

/// <summary>
/// The tunable thresholds for the karat/assay pipeline, carrying the legacy default values
/// (from <c>ConfigProvider</c>). Immutable; construct with <c>with</c> expressions to override
/// individual thresholds. See <see cref="Default"/> for the legacy defaults.
/// </summary>
public sealed record AssayOptions
{
    /// <summary>Gets the shared options instance carrying the legacy defaults.</summary>
    public static AssayOptions Default { get; } = new();

    /// <summary>
    /// Rejection threshold (percent, exclusive) for the general disallowed-alloy elements
    /// (W, Pt, Ir, Ru, Pd, Pb, Mo, Bi, Cd, In, Mn). Legacy <c>ElementPercentageToRejectItem</c>, default 2.
    /// </summary>
    public decimal ElementRejectPercent { get; init; } = 2m;

    /// <summary>Rejection threshold (percent, exclusive) for rhodium. Legacy hard-coded, default 4.</summary>
    public decimal RhodiumRejectPercent { get; init; } = 4m;

    /// <summary>Rejection threshold (percent, exclusive) for iron. Legacy hard-coded, default 10.</summary>
    public decimal IronRejectPercent { get; init; } = 10m;

    /// <summary>Minimum acceptable gold karat for the offer. Legacy <c>MinimumAcceptableGoldKarat</c>, default 9.2.</summary>
    public decimal MinGoldKarat { get; init; } = 9.2m;

    /// <summary>Minimum acceptable silver concentration (percent). Legacy <c>MinimumAcceptableSilverPercentage</c>, default 80.</summary>
    public decimal MinSilverPercent { get; init; } = 80m;

    /// <summary>
    /// When set, the offer uses <see cref="KaratResult.KaratUsingXrfPercentage"/>; otherwise
    /// <see cref="KaratResult.KaratUsingDensity"/>. Legacy <c>UseKaratUsingXrfPercentageForOffer</c>, default true.
    /// </summary>
    public bool UseKaratUsingXrfPercentageForOffer { get; init; } = true;

    /// <summary>
    /// Weight (grams, exclusive) above which the volume correction adjusts element percentages
    /// instead of the weight. Legacy <c>AcceptableWeightToNotConsiderGoldPercentageReduction</c>, default 10.
    /// </summary>
    public decimal HeavyItemWeightGrams { get; init; } = 10m;

    /// <summary>
    /// Weight (grams, exclusive) above which an out-of-range volume triggers rejection.
    /// Legacy <c>RejectOnVolumeErrorItemWeightThreshold</c>, default 1.
    /// </summary>
    public decimal VolumeRejectWeightGrams { get; init; } = 1m;

    /// <summary>
    /// Minimum calculated volume (cm³, exclusive) below which the volume fraud cross-check is skipped.
    /// Legacy <c>CalculatedVolumeThresholdLimit</c>, default 0.5.
    /// </summary>
    public decimal CalculatedVolumeThresholdLimit { get; init; } = 0.5m;

    /// <summary>
    /// Minimum correction factor (percent) for the volume to be considered in range.
    /// Legacy hard-coded, default 65.
    /// </summary>
    public decimal VolumeInRangeMinPercent { get; init; } = 65m;

    /// <summary>
    /// Upper correction factor (percent) of the heavy-item element-scaling band.
    /// Legacy hard-coded, default 89.
    /// </summary>
    public decimal HeavyScalingMaxPercent { get; init; } = 89m;

    /// <summary>Lower correction-factor ratio of the 1–10 g weight-adjustment band. Legacy hard-coded, default 0.65.</summary>
    public decimal WeightAdjustLowerRatio { get; init; } = 0.65m;

    /// <summary>Upper correction-factor ratio of the 1–10 g weight-adjustment band. Legacy hard-coded, default 0.89.</summary>
    public decimal WeightAdjustUpperRatio { get; init; } = 0.89m;

    /// <summary>
    /// Ratio ceiling (exclusive) below which the 1–10 g weight adjustment is applied.
    /// Legacy <c>WeightAdjustmentVolumeRatioThreshold</c>, default 0.95.
    /// </summary>
    public decimal WeightAdjustmentRatioThreshold { get; init; } = 0.95m;
}
