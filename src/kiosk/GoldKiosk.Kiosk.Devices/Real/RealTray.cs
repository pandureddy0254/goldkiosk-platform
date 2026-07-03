using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real tray driver stub. Phase 4 target: tray motor via the machine's relay/stepper
/// outputs with the tray-closed sensor bit (Advantech USB-4761) confirming end of travel.
/// </summary>
public sealed class RealTray : RealDeviceStub, ITray
{
    /// <summary>Initializes the stub.</summary>
    public RealTray()
        : base(DeviceKeys.Tray, "Customer tray (relay/stepper drive)", isCritical: true)
    {
    }

    /// <inheritdoc />
    public TrayState State => TrayState.Closed;

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default) =>
        throw NotWired();

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default) =>
        throw NotWired();
}
