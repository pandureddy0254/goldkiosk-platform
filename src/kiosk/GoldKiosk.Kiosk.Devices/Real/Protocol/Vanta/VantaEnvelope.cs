namespace GoldKiosk.Kiosk.Devices.Real.Protocol.Vanta;

/// <summary>
/// One JSON message from the Vanta WebSocket (camelCase on the wire via web-default
/// serialization). Command responses echo their command id; notifications use command id 403
/// with the subtype in <see cref="Id"/>.
/// </summary>
public sealed record VantaEnvelope
{
    /// <summary>The command id (see <see cref="VantaCommandId"/>).</summary>
    public int CommandId { get; init; }

    /// <summary>Correlation id, or the <see cref="VantaNotificationId"/> for notifications.</summary>
    public int Id { get; init; }

    /// <summary>Error payload for <see cref="VantaNotificationId.ErrorDuringTest"/>.</summary>
    public VantaError? Error { get; init; }

    /// <summary>Notification parameters (result payloads).</summary>
    public VantaEnvelopeParams? Params { get; init; }
}
