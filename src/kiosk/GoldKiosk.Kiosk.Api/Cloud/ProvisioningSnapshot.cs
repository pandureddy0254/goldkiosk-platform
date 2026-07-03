using GoldKiosk.Kiosk.Core.Cloud;

namespace GoldKiosk.Kiosk.Api.Cloud;

/// <summary>
/// An immutable snapshot of the edge's provisioning posture at a point in time.
/// </summary>
/// <param name="State">The current provisioning state.</param>
/// <param name="Provisioning">The last-known provisioning facts, when any were obtained.</param>
/// <param name="UpdatedAt">When this snapshot was produced, when known.</param>
public sealed record ProvisioningSnapshot(
    ProvisioningState State,
    KioskProvisioning? Provisioning,
    DateTimeOffset? UpdatedAt)
{
    /// <summary>The starting snapshot before any provisioning attempt has run.</summary>
    public static ProvisioningSnapshot Initial { get; } =
        new(ProvisioningState.Unprovisioned, Provisioning: null, UpdatedAt: null);
}
