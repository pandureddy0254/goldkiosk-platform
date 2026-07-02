namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>Logical camera roles; configuration maps each role to a physical camera.</summary>
public enum CameraRole
{
    /// <summary>Customer-facing camera (KYC selfie / face match).</summary>
    Customer,

    /// <summary>Tray camera (item-presence and tray-state shots).</summary>
    Tray,

    /// <summary>Item photo at insertion (audit record).</summary>
    ItemInsert,

    /// <summary>Item photo at return on the decline/rejection path (audit record).</summary>
    ItemReturn,

    /// <summary>High-resolution item shot for live-agent escalation.</summary>
    LiveAgent,
}
