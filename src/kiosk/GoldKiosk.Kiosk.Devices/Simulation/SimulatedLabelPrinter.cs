using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>Simulated label printer: printing completes after a fixed scaled delay.</summary>
public sealed class SimulatedLabelPrinter : SimulatedDeviceBase, ILabelPrinter
{
    /// <summary>Initializes the simulated printer.</summary>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays.</param>
    public SimulatedLabelPrinter(SimulationOptions simulation, TimeProvider timeProvider)
        : base(DeviceKeys.LabelPrinter, "Label printer (simulated)", isCritical: false, simulation, timeProvider)
    {
    }

    /// <inheritdoc />
    public async Task PrintAsync(BagLabel label, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(label);
        ThrowIfFaulted();
        await DelayAsync(1200, cancellationToken).ConfigureAwait(false);
    }
}
