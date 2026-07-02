using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Tuning for simulated devices, bound from <c>Devices:Simulation</c> by the host
/// (see <see cref="DevicesOptions"/> for binding guidance and config layering).
/// </summary>
public sealed class SimulationOptions
{
    /// <summary>
    /// Multiplier applied to every simulated operation latency. <c>1.0</c> approximates real
    /// hardware timing; <c>0</c> removes delays for fast automated runs.
    /// </summary>
    [Range(0.0, 1000.0)]
    public double LatencyMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Device keys (see <c>DeviceKeys</c>, case-insensitive) whose simulators inject faults:
    /// connect succeeds, operations throw <c>SimulatedDeviceFaultException</c> — the scripted
    /// failure mode of ADR 0004.
    /// </summary>
    public IList<string> FaultDevices { get; init; } = [];
}
