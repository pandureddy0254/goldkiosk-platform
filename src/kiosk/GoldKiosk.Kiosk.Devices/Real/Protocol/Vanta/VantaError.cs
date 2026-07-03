namespace GoldKiosk.Kiosk.Devices.Real.Protocol.Vanta;

/// <summary>Error payload of a Vanta error notification.</summary>
public sealed record VantaError
{
    /// <summary>Vendor error code.</summary>
    public int ErrorCode { get; init; }

    /// <summary>Human-readable error text from the gun.</summary>
    public string? ErrorString { get; init; }

    /// <summary>Vendor error category.</summary>
    public int ErrorType { get; init; }
}
