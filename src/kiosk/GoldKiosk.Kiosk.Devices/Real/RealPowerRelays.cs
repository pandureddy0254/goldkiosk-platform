using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real power relay driver stub. Phase 4 target SDK: Advantech USB-4761 DAQ digital outputs,
/// including the XRF analyser power-cycle relay.
/// </summary>
public sealed class RealPowerRelays : RealDeviceStub, IPowerRelays
{
    /// <summary>Initializes the stub.</summary>
    public RealPowerRelays()
        : base(DeviceKeys.PowerRelays, "Power relays (Advantech USB-4761)", isCritical: true)
    {
    }

    /// <inheritdoc />
    public Task SetDevicePowerAsync(bool on, CancellationToken cancellationToken = default) =>
        throw NotWired();

    /// <inheritdoc />
    public Task ToggleAnalyserPowerAsync(CancellationToken cancellationToken = default) =>
        throw NotWired();
}
