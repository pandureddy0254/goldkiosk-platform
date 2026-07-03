using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>
/// Simulated volume chamber: derives a volume from the supplied weight using a 22-karat gold
/// density (17.7 g/cc), so the density cross-check agrees with the simulated scale and
/// analyser (12.4 g → 0.70 cc).
/// </summary>
public sealed class SimulatedVolumeChamber : SimulatedDeviceBase, IVolumeChamber
{
    private const decimal TwentyTwoKaratDensityGramsPerCc = 17.7m;

    /// <summary>Initializes the simulated chamber.</summary>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays.</param>
    public SimulatedVolumeChamber(SimulationOptions simulation, TimeProvider timeProvider)
        : base(DeviceKeys.VolumeChamber, "Volume chamber (simulated)", isCritical: true, simulation, timeProvider)
    {
    }

    /// <inheritdoc />
    public async Task<VolumeReading> MeasureAsync(decimal weightGrams, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(weightGrams);
        ThrowIfFaulted();
        await DelayAsync(2000, cancellationToken).ConfigureAwait(false);

        decimal volumeCc = weightGrams == 0m
            ? 0m
            : decimal.Round(weightGrams / TwentyTwoKaratDensityGramsPerCc, 2, MidpointRounding.AwayFromZero);
        return new VolumeReading(volumeCc, Calibrated: true);
    }
}
