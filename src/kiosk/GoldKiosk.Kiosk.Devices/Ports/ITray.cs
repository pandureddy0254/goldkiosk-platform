using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// Customer item tray port — the motorized drawer the customer places their item on.
/// Real target: tray motor driven through the machine's relay/stepper outputs with the
/// tray-closed sensor bit (see <see cref="ISensorBoard"/>) confirming end of travel.
/// </summary>
public interface ITray : IKioskDevice
{
    /// <summary>The tray's current physical state.</summary>
    TrayState State { get; }

    /// <summary>Opens the tray and returns when it reports fully open.</summary>
    /// <param name="cancellationToken">Cancels waiting for the motion (the tray finishes its travel safely).</param>
    /// <returns>A task that completes when the tray is open.</returns>
    Task OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>Closes the tray and returns when it reports fully closed.</summary>
    /// <param name="cancellationToken">Cancels waiting for the motion (the tray finishes its travel safely).</param>
    /// <returns>A task that completes when the tray is closed.</returns>
    Task CloseAsync(CancellationToken cancellationToken = default);
}
