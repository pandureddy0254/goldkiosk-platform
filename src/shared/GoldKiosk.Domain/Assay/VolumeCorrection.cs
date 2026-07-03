namespace GoldKiosk.Domain.Assay;

/// <summary>
/// The kind of volume correction the fraud cross-check recommends for an item whose measured
/// volume exceeds its calculated volume.
/// </summary>
public enum VolumeCorrection
{
    /// <summary>No correction applies.</summary>
    None,

    /// <summary>
    /// Heavy item (weight above <see cref="AssayOptions.HeavyItemWeightGrams"/>): scale the XRF
    /// element percentages by the correction factor and recompute the karat.
    /// </summary>
    HeavyElementScaling,

    /// <summary>
    /// Light item (1–10 g band): scale the paid weight by the correction-factor ratio instead of
    /// the element percentages.
    /// </summary>
    WeightAdjustment,
}
