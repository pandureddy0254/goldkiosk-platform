namespace GoldKiosk.Kiosk.Devices.Real.Protocol.Vanta;

/// <summary>One element reading in a Vanta chemistry result.</summary>
public sealed record VantaElement
{
    /// <summary>Concentration in percent.</summary>
    public double Concentration { get; init; }

    /// <summary>Element symbol (e.g. <c>Au</c>, <c>Ag</c>).</summary>
    public string? ElementName { get; init; }

    /// <summary>Reported measurement error.</summary>
    public double Error { get; init; }
}
