using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real.Protocol.Vanta;

/// <summary>
/// Maps the final Vanta result blobs of one analysis run to an <see cref="AnalysisRun"/>.
/// Ported from the legacy pipeline: <c>VantaMappingExtension.MapToAnalysisPayload</c>
/// (element extraction) + <c>MetalAnalyserCommandExecutorNew.Merge</c> (blob merging:
/// discard blobs containing a 100 %-concentration element with error ≥ 15, then average
/// per-element across the remaining blobs).
/// </summary>
public static class VantaChemistryMapper
{
    private const double SaturatedConcentration = 100d;
    private const double SaturatedErrorThreshold = 15d;

    private static readonly string[] _notPlatedIndicators = ["none", "no", "0", "false", "notplated"];

    /// <summary>Maps the run's final results (usually one blob, occasionally more) to the port contract.</summary>
    /// <param name="finalResults">Every result blob received with <c>analysis.final == true</c>.</param>
    /// <returns>The completed run; failed when no usable chemistry was produced.</returns>
    public static AnalysisRun Map(IReadOnlyList<VantaResult> finalResults)
    {
        ArgumentNullException.ThrowIfNull(finalResults);

        if (finalResults.Count == 0)
        {
            return Failed("analysis_no_result");
        }

        if (finalResults.All(result => result.Analysis?.StatusId != 0))
        {
            return Failed($"analysis_status:{finalResults[^1].Analysis?.StatusId ?? -1}");
        }

        List<VantaResult> usable = [.. finalResults
            .Where(result => result.Analysis?.StatusId == 0 && result.Chemistry is { Count: > 0 })
            .Where(result => !result.Chemistry!.Any(element =>
                element.Concentration == SaturatedConcentration && element.Error >= SaturatedErrorThreshold))];
        if (usable.Count == 0)
        {
            return Failed("analysis_no_chemistry");
        }

        Dictionary<string, decimal> elements = usable
            .SelectMany(result => result.Chemistry!)
            .Where(element => !string.IsNullOrWhiteSpace(element.ElementName))
            .GroupBy(element => Normalize(element.ElementName!), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => Math.Round((decimal)group.Average(element => element.Concentration), 4),
                StringComparer.Ordinal);
        if (elements.Count == 0)
        {
            return Failed("analysis_no_chemistry");
        }

        return new AnalysisRun(
            Succeeded: true,
            FailureReason: null,
            GoldPercent: elements.TryGetValue("Au", out decimal gold) ? gold : null,
            SilverPercent: elements.TryGetValue("Ag", out decimal silver) ? silver : null,
            GoldPlated: finalResults.Any(result => IndicatesPlating(result.Analysis?.AuPlating)),
            Elements: elements);
    }

    /// <summary>
    /// Gold-plating heuristic over the gun's free-form <c>auPlating</c> string: any non-empty
    /// value that is not an explicit not-plated indicator counts as plated. Downstream the
    /// flag rejects the item outright (legacy <c>GCKaratCalculator</c>:
    /// <c>IsGoldPlated → RejectionReason.GoldPlated</c>) — coating detection errs cautious.
    /// </summary>
    /// <param name="auPlating">The wire value.</param>
    /// <returns><see langword="true"/> when the reading indicates surface plating.</returns>
    public static bool IndicatesPlating(string? auPlating)
    {
        if (string.IsNullOrWhiteSpace(auPlating))
        {
            return false;
        }

        string normalized = auPlating.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
        return !_notPlatedIndicators.Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    private static string Normalize(string symbol)
    {
        string trimmed = symbol.Trim();
        return trimmed.Length switch
        {
            0 => trimmed,
            1 => trimmed.ToUpperInvariant(),
            _ => char.ToUpperInvariant(trimmed[0]) + trimmed[1..].ToLowerInvariant(),
        };
    }

    private static AnalysisRun Failed(string reason) => new(
        Succeeded: false,
        FailureReason: reason,
        GoldPercent: null,
        SilverPercent: null,
        GoldPlated: false,
        Elements: new Dictionary<string, decimal>());
}
