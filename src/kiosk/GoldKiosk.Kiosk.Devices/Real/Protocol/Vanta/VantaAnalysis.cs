namespace GoldKiosk.Kiosk.Devices.Real.Protocol.Vanta;

/// <summary>Status block of a Vanta analysis result.</summary>
public sealed record VantaAnalysis
{
    /// <summary>
    /// The gun's gold-plating indicator (wire field <c>auPlating</c>). Free-form string; see
    /// <see cref="VantaChemistryMapper"/> for the heuristic mapping to a boolean.
    /// </summary>
    public string? AuPlating { get; init; }

    /// <summary><see langword="true"/> for the final result of the exposure.</summary>
    public bool Final { get; init; }

    /// <summary>0 = success; anything else is a failed analysis.</summary>
    public int StatusId { get; init; }
}
