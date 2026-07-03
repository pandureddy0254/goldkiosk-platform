namespace GoldKiosk.Kiosk.Devices.Real.Protocol.Vanta;

/// <summary>Parameters of a Vanta notification envelope.</summary>
public sealed record VantaEnvelopeParams
{
    /// <summary>The analysis result carried by a <c>ResultReceived</c> notification.</summary>
    public VantaResult? Result { get; init; }
}
