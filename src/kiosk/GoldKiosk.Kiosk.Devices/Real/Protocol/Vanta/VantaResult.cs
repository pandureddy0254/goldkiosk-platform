namespace GoldKiosk.Kiosk.Devices.Real.Protocol.Vanta;

/// <summary>One Vanta analysis result blob (intermediate or final).</summary>
public sealed record VantaResult
{
    /// <summary>Analysis status (final flag, plating indicator).</summary>
    public VantaAnalysis? Analysis { get; init; }

    /// <summary>Elemental composition when the analysis produced one.</summary>
    public IReadOnlyList<VantaElement>? Chemistry { get; init; }
}
