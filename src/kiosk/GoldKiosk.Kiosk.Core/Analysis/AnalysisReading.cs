namespace GoldKiosk.Kiosk.Core.Analysis;

/// <summary>
/// The measured item facts collected by the analysis pipeline (scale, XRF analyser,
/// volume chamber), expressed with BCL types only so Kiosk.Core stays free of device types.
/// </summary>
/// <param name="WeightGrams">The stable item weight in grams.</param>
/// <param name="GoldPercent">Gold content in percent (e.g. 91.6 for 22K), when detected.</param>
/// <param name="SilverPercent">Silver content in percent, when detected.</param>
/// <param name="GoldPlated"><see langword="true"/> when the surface reading indicates plating.</param>
/// <param name="Elements">Full elemental composition, symbol → percent (e.g. <c>"Au" → 91.6</c>).</param>
/// <param name="MeasuredVolumeCc">The chamber-measured volume in cubic centimetres.</param>
/// <param name="VolumeCalibrated"><see langword="false"/> when the chamber reading is advisory only.</param>
public sealed record AnalysisReading(
    decimal WeightGrams,
    decimal? GoldPercent,
    decimal? SilverPercent,
    bool GoldPlated,
    IReadOnlyDictionary<string, decimal> Elements,
    decimal MeasuredVolumeCc,
    bool VolumeCalibrated);
