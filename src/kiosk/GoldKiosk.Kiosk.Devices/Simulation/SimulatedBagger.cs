using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>
/// Simulated bagger: reports <see cref="BaggerStatus.Bagging"/> while a bag cycle runs and
/// <see cref="BaggerStatus.Idle"/> otherwise, honouring the new-transaction interlock.
/// </summary>
public sealed class SimulatedBagger : SimulatedDeviceBase, IBagger
{
    private BaggerStatus _status = BaggerStatus.Idle;

    /// <summary>Initializes the simulated bagger.</summary>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays.</param>
    public SimulatedBagger(SimulationOptions simulation, TimeProvider timeProvider)
        : base(DeviceKeys.Bagger, "Bagger (simulated)", isCritical: true, simulation, timeProvider)
    {
    }

    /// <inheritdoc />
    public async Task<BaggerStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfFaulted();
        await DelayAsync(50, cancellationToken).ConfigureAwait(false);
        return _status;
    }

    /// <inheritdoc />
    public async Task BagItemAsync(string bagNumber, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bagNumber);
        ThrowIfFaulted();

        _status = BaggerStatus.Bagging;
        SetHealth(DeviceState.Busy, $"Bagging {bagNumber}.");
        try
        {
            await DelayAsync(3000, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _status = BaggerStatus.Idle;
            SetHealth(DeviceState.Ready);
        }
    }
}
