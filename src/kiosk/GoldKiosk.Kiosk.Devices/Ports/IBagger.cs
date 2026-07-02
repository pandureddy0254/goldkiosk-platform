using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// Bagging unit port (arm-driven item bagging with a status interlock: no new transaction
/// starts while bagging is in progress — legacy <c>BAGGER_GET_STATUS</c> parity).
/// </summary>
public interface IBagger : IKioskDevice
{
    /// <summary>Reads the bagger's current status for the session interlock.</summary>
    /// <param name="cancellationToken">Cancels the status read.</param>
    /// <returns>The current bagger status.</returns>
    Task<BaggerStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Bags the item currently in the bagging position.</summary>
    /// <param name="bagNumber">The bag identifier printed on the label.</param>
    /// <param name="cancellationToken">Cancels waiting for completion (the mechanism finishes its cycle safely).</param>
    /// <returns>A task that completes when the item is sealed in the bag.</returns>
    Task BagItemAsync(string bagNumber, CancellationToken cancellationToken = default);
}
