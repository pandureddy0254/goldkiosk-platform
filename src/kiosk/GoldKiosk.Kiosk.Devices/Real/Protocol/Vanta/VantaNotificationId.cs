namespace GoldKiosk.Kiosk.Devices.Real.Protocol.Vanta;

/// <summary>Notification subtype ids inside commandId 403 envelopes (legacy <c>VantaNotificationType</c>).</summary>
public enum VantaNotificationId
{
    /// <summary>General system status.</summary>
    SystemStatus = 100,

    /// <summary>Battery status heartbeat — its freshness is the gun's liveness signal.</summary>
    BatteryStatus = 103,

    /// <summary>Analysis result (intermediate while <c>final</c> is false, then the final chemistry).</summary>
    ResultReceived = 206,

    /// <summary>Exposure progress.</summary>
    ExposureStatus = 207,

    /// <summary>Error raised during a test.</summary>
    ErrorDuringTest = 209,
}
