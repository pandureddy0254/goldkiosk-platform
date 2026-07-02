using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>
/// Simulated precision scale: always returns a stable 12.4 g reading — the canonical
/// happy-path item weight shared with the other simulators (analyser, volume chamber).
/// </summary>
public sealed class SimulatedScale : SimulatedDeviceBase, IScale
{
    /// <summary>The deterministic happy-path item weight in grams.</summary>
    public const decimal HappyPathGrams = 12.4m;

    /// <summary>Initializes the simulated scale.</summary>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays.</param>
    public SimulatedScale(SimulationOptions simulation, TimeProvider timeProvider)
        : base(DeviceKeys.Scale, "Precision scale (simulated)", isCritical: true, simulation, timeProvider)
    {
    }

    /// <inheritdoc />
    public async Task<WeightReading> GetWeightAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfFaulted();
        await DelayAsync(400, cancellationToken).ConfigureAwait(false);
        return new WeightReading(HappyPathGrams, Stable: true);
    }

    /// <inheritdoc />
    public async Task ZeroAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfFaulted();
        await DelayAsync(300, cancellationToken).ConfigureAwait(false);
    }
}
