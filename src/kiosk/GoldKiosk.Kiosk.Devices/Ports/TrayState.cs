namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>Physical position of the customer item tray.</summary>
public enum TrayState
{
    /// <summary>The tray is fully open; the customer can place or remove an item.</summary>
    Open,

    /// <summary>The tray is fully closed and interlocked.</summary>
    Closed,

    /// <summary>The tray is in motion (opening or closing).</summary>
    Moving,

    /// <summary>The tray is obstructed and cannot complete its motion.</summary>
    Blocked,
}
