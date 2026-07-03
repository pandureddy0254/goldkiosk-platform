using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Core.Options;

/// <summary>
/// Options for the <c>Analysis</c> configuration section — item acceptance thresholds
/// applied by <see cref="Analysis.AnalysisPolicy"/> after the hardware pipeline runs.
/// </summary>
public sealed class AnalysisOptions
{
    /// <summary>The configuration section name this options class binds from.</summary>
    public const string SectionName = "Analysis";

    /// <summary>The minimum accepted item weight in grams; lighter items are rejected.</summary>
    [Range(0.1, 1000.0)]
    public decimal MinWeightGrams { get; set; } = 1.0m;

    /// <summary>
    /// The minimum accepted gold content in percent (33.3 ≈ the 8-karat legal floor).
    /// </summary>
    [Range(0.0, 100.0)]
    public decimal MinGoldPercent { get; set; } = 33.3m;

    /// <summary>
    /// Maximum allowed deviation, in percent, between the measured volume and the volume
    /// calculated from weight and XRF composition (the legacy fraud cross-check).
    /// </summary>
    [Range(1.0, 100.0)]
    public decimal VolumeTolerancePercent { get; set; } = 25.0m;
}
