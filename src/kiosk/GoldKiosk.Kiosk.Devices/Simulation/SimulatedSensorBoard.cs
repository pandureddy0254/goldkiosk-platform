using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>
/// Simulated sensor board: reports the trading-ready snapshot — tray and chamber closed,
/// UPS on mains, both cups seated.
/// </summary>
public sealed class SimulatedSensorBoard : SimulatedDeviceBase, ISensorBoard
{
    /// <summary>Initializes the simulated sensor board.</summary>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays.</param>
    public SimulatedSensorBoard(SimulationOptions simulation, TimeProvider timeProvider)
        : base(DeviceKeys.SensorBoard, "Sensor board (simulated)", isCritical: true, simulation, timeProvider)
    {
    }

    /// <inheritdoc />
    public async Task<SensorSnapshot> ReadAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfFaulted();
        await DelayAsync(50, cancellationToken).ConfigureAwait(false);
        return new SensorSnapshot(
            TrayClosed: true,
            ChamberClosed: true,
            UpsOnMains: true,
            ChamberCupPresent: true,
            ScaleCupPresent: true);
    }
}
