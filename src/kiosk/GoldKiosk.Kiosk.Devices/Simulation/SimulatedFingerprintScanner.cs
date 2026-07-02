using System.Text;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>
/// Simulated fingerprint scanner: succeeds on the first sample with a deterministic
/// placeholder template.
/// </summary>
public sealed class SimulatedFingerprintScanner : SimulatedDeviceBase, IFingerprintScanner
{
    private static readonly byte[] _template = Encoding.ASCII.GetBytes("SIMULATED-FP-TEMPLATE-V1");

    /// <summary>Initializes the simulated fingerprint scanner.</summary>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays.</param>
    public SimulatedFingerprintScanner(SimulationOptions simulation, TimeProvider timeProvider)
        : base(DeviceKeys.FingerprintScanner, "Fingerprint scanner (simulated)", isCritical: false, simulation, timeProvider)
    {
    }

    /// <inheritdoc />
    public async Task<FingerprintResult> CaptureAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfFaulted();
        await DelayAsync(900, cancellationToken).ConfigureAwait(false);
        return new FingerprintResult(Succeeded: true, SamplesNeeded: 1, Template: _template);
    }
}
