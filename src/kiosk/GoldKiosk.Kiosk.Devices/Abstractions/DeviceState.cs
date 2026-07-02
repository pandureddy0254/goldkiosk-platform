namespace GoldKiosk.Kiosk.Devices.Abstractions;

/// <summary>Lifecycle state of a kiosk device connection.</summary>
public enum DeviceState
{
    /// <summary>Constructed but <c>ConnectAsync</c> has not been called yet.</summary>
    NotInitialized,

    /// <summary>Connection / initialization is in progress.</summary>
    Connecting,

    /// <summary>Connected and idle — ready to accept operations.</summary>
    Ready,

    /// <summary>Connected and currently executing an operation.</summary>
    Busy,

    /// <summary>An unrecoverable error was reported; operations will fail until reconnect.</summary>
    Faulted,

    /// <summary>Cleanly disconnected via <c>DisconnectAsync</c>.</summary>
    Disconnected,
}
