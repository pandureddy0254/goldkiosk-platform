namespace GoldKiosk.Kiosk.Api.Cloud;

/// <summary>
/// The edge's cloud-provisioning posture, readable by the rest of the kiosk (e.g. to show an
/// out-of-service screen). Never blocks startup — the kiosk boots and reflects whichever
/// state it resolved.
/// </summary>
public enum ProvisioningState
{
    /// <summary>Cloud integration is off (<c>Cloud:Enabled=false</c>); the kiosk runs on local mock pricing.</summary>
    Disabled,

    /// <summary>Cloud is enabled but no provisioning has been obtained yet and no cache exists.</summary>
    Unprovisioned,

    /// <summary>Provisioned and cleared to trade (<c>can_trade=true</c>).</summary>
    Provisioned,

    /// <summary>Provisioned but the cloud says this kiosk may not trade (maintenance / not live).</summary>
    OutOfService,

    /// <summary>Cloud unreachable; operating on the last-good provisioning cache.</summary>
    Offline,
}
