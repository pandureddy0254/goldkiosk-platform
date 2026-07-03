namespace GoldKiosk.Kiosk.Devices.Real.Protocol.Vanta;

/// <summary>Raw (non-JSON) text messages of the Vanta controller handshake (legacy <c>VantaStringIdentifiers</c>).</summary>
public static class VantaProtocolStrings
{
    /// <summary>Controller handshake sent immediately after the WebSocket opens.</summary>
    public const string Handshake = "Hi i am a controller";

    /// <summary>Gun response indicating it accepts commands.</summary>
    public const string ReadyToReceiveCommands = "Server ready to receive commands.";

    /// <summary>Gun response when another controller owns the device.</summary>
    public const string DeviceInUseByOther = "Rejecting connection. Device being controlled by ";

    /// <summary>Gun response when another user session is active.</summary>
    public const string OtherUserLoggedIn = "Rejecting connection. User is Logged In.";

    /// <summary>UDP heartbeat datagram payload sent every second.</summary>
    public const string HeartbeatMessage = "MsgFromPC";
}
