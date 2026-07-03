using GoldKiosk.Kiosk.Core.Cloud;

namespace GoldKiosk.Kiosk.Api.Cloud;

/// <summary>
/// Thread-safe holder for the edge's current provisioning posture, published by
/// <see cref="CloudProvisioningService"/> and read by the rest of the kiosk through a simple
/// accessor. Registered as a singleton; reads never block and never throw.
/// </summary>
public sealed class CurrentProvisioning
{
    private volatile ProvisioningSnapshot _snapshot = ProvisioningSnapshot.Initial;

    /// <summary>The current provisioning snapshot.</summary>
    public ProvisioningSnapshot Snapshot => _snapshot;

    /// <summary>The last-known provisioning facts, or <see langword="null"/> when none obtained.</summary>
    public KioskProvisioning? Provisioning => _snapshot.Provisioning;

    /// <summary>The current provisioning state.</summary>
    public ProvisioningState State => _snapshot.State;

    /// <summary>Whether the cloud has flagged this kiosk as unable to trade (UI shows out-of-service).</summary>
    public bool IsOutOfService => _snapshot.State == ProvisioningState.OutOfService;

    /// <summary>Publishes a new snapshot.</summary>
    /// <param name="snapshot">The snapshot to publish.</param>
    public void Set(ProvisioningSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _snapshot = snapshot;
    }
}
