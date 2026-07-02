using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>Simulated robotic arm: every named movement completes after a fixed scaled delay.</summary>
public sealed class SimulatedRoboticArm : SimulatedDeviceBase, IRoboticArm
{
    /// <summary>Initializes the simulated arm.</summary>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays.</param>
    public SimulatedRoboticArm(SimulationOptions simulation, TimeProvider timeProvider)
        : base(DeviceKeys.RoboticArm, "Robotic arm (simulated)", isCritical: true, simulation, timeProvider)
    {
    }

    /// <inheritdoc />
    public async Task MoveAsync(ArmMove move, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(move))
        {
            throw new ArgumentOutOfRangeException(nameof(move), move, "Unknown arm movement.");
        }

        ThrowIfFaulted();
        SetHealth(DeviceState.Busy, $"Executing {move}.");
        try
        {
            await DelayAsync(1200, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            SetHealth(DeviceState.Ready);
        }
    }
}
