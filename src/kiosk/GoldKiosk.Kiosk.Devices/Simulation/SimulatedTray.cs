using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>
/// Simulated customer tray: starts <see cref="TrayState.Closed"/>, passes through
/// <see cref="TrayState.Moving"/> during each commanded motion, and always completes.
/// </summary>
public sealed class SimulatedTray : SimulatedDeviceBase, ITray
{
    /// <summary>Initializes the simulated tray.</summary>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays.</param>
    public SimulatedTray(SimulationOptions simulation, TimeProvider timeProvider)
        : base(DeviceKeys.Tray, "Customer tray (simulated)", isCritical: true, simulation, timeProvider)
    {
    }

    /// <inheritdoc />
    public TrayState State { get; private set; } = TrayState.Closed;

    /// <inheritdoc />
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfFaulted();
        State = TrayState.Moving;
        SetHealth(DeviceState.Busy, "Tray opening.");
        try
        {
            await DelayAsync(1500, cancellationToken).ConfigureAwait(false);
            State = TrayState.Open;
        }
        finally
        {
            SetHealth(DeviceState.Ready);
        }
    }

    /// <inheritdoc />
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfFaulted();
        State = TrayState.Moving;
        SetHealth(DeviceState.Busy, "Tray closing.");
        try
        {
            await DelayAsync(1500, cancellationToken).ConfigureAwait(false);
            State = TrayState.Closed;
        }
        finally
        {
            SetHealth(DeviceState.Ready);
        }
    }
}
