using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>Simulated power relays: switching completes after a fixed scaled delay.</summary>
public sealed class SimulatedPowerRelays : SimulatedDeviceBase, IPowerRelays
{
    /// <summary>Initializes the simulated relays.</summary>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays.</param>
    public SimulatedPowerRelays(SimulationOptions simulation, TimeProvider timeProvider)
        : base(DeviceKeys.PowerRelays, "Power relays (simulated)", isCritical: true, simulation, timeProvider)
    {
    }

    /// <inheritdoc />
    public async Task SetDevicePowerAsync(bool on, CancellationToken cancellationToken = default)
    {
        ThrowIfFaulted();
        await DelayAsync(100, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ToggleAnalyserPowerAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfFaulted();
        await DelayAsync(1500, cancellationToken).ConfigureAwait(false);
    }
}
