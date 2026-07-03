namespace GoldKiosk.Domain.Assay;

/// <summary>
/// Pure rejection policy ported from the legacy <c>GCKaratCalculator</c> /
/// <c>GCMetalAnalysisPayload</c> threshold checks. Applies the disallowed-alloy thresholds and
/// the karat/silver floors and reports the resulting <see cref="RejectionReason"/>s in the
/// legacy evaluation order.
/// </summary>
/// <remarks>
/// The volume fraud rejection (<see cref="RejectionReason.UnacceptableVolumeError"/>) is produced
/// separately by <see cref="VolumeCrossCheck"/>; callers compose the two outcomes.
/// </remarks>
public sealed class AssayRejectionPolicy
{
    private readonly AssayOptions _options;

    /// <summary>Creates a policy using the given thresholds.</summary>
    /// <param name="options">The assay thresholds (see <see cref="AssayOptions.Default"/>).</param>
    public AssayRejectionPolicy(AssayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>Gets a shared policy using the legacy default thresholds.</summary>
    public static AssayRejectionPolicy Default { get; } = new(AssayOptions.Default);

    /// <summary>
    /// Evaluates the readings and floors and returns the first (highest-priority) rejection
    /// reason, or <see langword="null"/> to accept.
    /// </summary>
    /// <param name="readings">The XRF element readings.</param>
    /// <param name="offerKarat">The karat used for the offer; when supplied and below <see cref="AssayOptions.MinGoldKarat"/> the item is rejected.</param>
    /// <param name="silverPercent">The silver concentration; when supplied and below <see cref="AssayOptions.MinSilverPercent"/> the item is rejected.</param>
    /// <param name="isGoldPlated">Whether the analyser flagged the item as gold plated.</param>
    /// <returns>The first applicable <see cref="RejectionReason"/>, or <see langword="null"/> when the item is accepted.</returns>
    public RejectionReason? Evaluate(
        IReadOnlyList<ElementReading> readings,
        decimal? offerKarat = null,
        decimal? silverPercent = null,
        bool isGoldPlated = false)
    {
        var reasons = EvaluateAll(readings, offerKarat, silverPercent, isGoldPlated);
        return reasons.Count > 0 ? reasons[0] : null;
    }

    /// <summary>
    /// Evaluates the readings and floors and returns every applicable rejection reason in the
    /// legacy evaluation order (an empty list means accept).
    /// </summary>
    /// <param name="readings">The XRF element readings.</param>
    /// <param name="offerKarat">The karat used for the offer; when supplied and below <see cref="AssayOptions.MinGoldKarat"/> the item is rejected.</param>
    /// <param name="silverPercent">The silver concentration; when supplied and below <see cref="AssayOptions.MinSilverPercent"/> the item is rejected.</param>
    /// <param name="isGoldPlated">Whether the analyser flagged the item as gold plated.</param>
    /// <returns>The applicable <see cref="RejectionReason"/>s in evaluation order.</returns>
    public IReadOnlyList<RejectionReason> EvaluateAll(
        IReadOnlyList<ElementReading> readings,
        decimal? offerKarat = null,
        decimal? silverPercent = null,
        bool isGoldPlated = false)
    {
        ArgumentNullException.ThrowIfNull(readings);

        var reasons = new List<RejectionReason>();
        var general = _options.ElementRejectPercent;

        AddIfExceeds(readings, reasons, "W", general, RejectionReason.ContainsTungsten);
        AddIfExceeds(readings, reasons, "Pt", general, RejectionReason.ContainsPlatinum);
        AddIfExceeds(readings, reasons, "Ir", general, RejectionReason.ContainsIridium);
        AddIfExceeds(readings, reasons, "Rh", _options.RhodiumRejectPercent, RejectionReason.ContainsRhodium);
        AddIfExceeds(readings, reasons, "Ru", general, RejectionReason.ContainsRuthenium);
        AddIfExceeds(readings, reasons, "Pd", general, RejectionReason.ContainsPalladium);
        AddIfExceeds(readings, reasons, "Pb", general, RejectionReason.ContainsLead);
        AddIfExceeds(readings, reasons, "Mo", general, RejectionReason.ContainsMolybdenum);
        AddIfExceeds(readings, reasons, "Bi", general, RejectionReason.ContainsBismuth);
        AddIfExceeds(readings, reasons, "Cd", general, RejectionReason.ContainsCadmium);
        AddIfExceeds(readings, reasons, "Fe", _options.IronRejectPercent, RejectionReason.ContainsIron);
        AddIfExceeds(readings, reasons, "Mn", general, RejectionReason.ContainsManganese);
        AddIfExceeds(readings, reasons, "In", general, RejectionReason.ContainsIndium);

        if (offerKarat is { } karat && karat < _options.MinGoldKarat)
        {
            reasons.Add(RejectionReason.LessThanAcceptableGoldKarat);
        }

        if (silverPercent is { } silver && silver < _options.MinSilverPercent)
        {
            reasons.Add(RejectionReason.LessThanAcceptableSilverPercent);
        }

        if (isGoldPlated)
        {
            reasons.Add(RejectionReason.GoldPlated);
        }

        return reasons;
    }

    private static void AddIfExceeds(
        IReadOnlyList<ElementReading> readings,
        List<RejectionReason> reasons,
        string symbol,
        decimal threshold,
        RejectionReason reason)
    {
        foreach (var reading in readings)
        {
            if (string.Equals(reading.Symbol, symbol, StringComparison.OrdinalIgnoreCase))
            {
                if (reading.Percent > threshold)
                {
                    reasons.Add(reason);
                }

                return;
            }
        }
    }
}
