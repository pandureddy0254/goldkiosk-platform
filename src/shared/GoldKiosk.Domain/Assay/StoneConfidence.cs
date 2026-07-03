namespace GoldKiosk.Domain.Assay;

/// <summary>
/// The confidence tier of a stone-weight estimate.
/// </summary>
/// <remarks>
/// Until the stone-weight model is calibrated against a reference set (see the vision/karat
/// rebuild design note §3), the estimator is <b>report-only</b>: the figure must never adjust the
/// paid weight. Only <see cref="ReportOnly"/> is currently emitted; richer tiers are reserved for
/// the calibrated model.
/// </remarks>
public enum StoneConfidence
{
    /// <summary>Uncalibrated, advisory-only estimate; must not adjust the paid weight.</summary>
    ReportOnly = 0,
}
