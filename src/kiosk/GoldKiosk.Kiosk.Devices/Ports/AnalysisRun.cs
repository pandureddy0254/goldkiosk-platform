namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>Outcome of one XRF analysis run.</summary>
/// <param name="Succeeded"><see langword="true"/> when the run produced a usable composition.</param>
/// <param name="FailureReason">Machine-readable failure reason when <paramref name="Succeeded"/> is <see langword="false"/>.</param>
/// <param name="GoldPercent">Gold content in percent (e.g. 91.6 for 22K), when detected.</param>
/// <param name="SilverPercent">Silver content in percent, when detected.</param>
/// <param name="GoldPlated"><see langword="true"/> when the surface reading indicates plating rather than solid gold.</param>
/// <param name="Elements">Full elemental composition, symbol → percent (e.g. <c>"Au" → 91.6</c>).</param>
public sealed record AnalysisRun(
    bool Succeeded,
    string? FailureReason,
    decimal? GoldPercent,
    decimal? SilverPercent,
    bool GoldPlated,
    IReadOnlyDictionary<string, decimal> Elements);
