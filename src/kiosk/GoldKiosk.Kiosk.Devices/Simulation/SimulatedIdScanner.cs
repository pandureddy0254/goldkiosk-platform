using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>
/// Simulated ID scanner: returns a deterministic adult (32 years old), non-expired
/// (valid 4 more years) government ID so the identity step and age gate pass. Dates derive
/// from the injected <see cref="System.TimeProvider"/>, never the system clock.
/// </summary>
public sealed class SimulatedIdScanner : SimulatedDeviceBase, IIdScanner
{
    /// <summary>Initializes the simulated ID scanner.</summary>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays and document dates.</param>
    public SimulatedIdScanner(SimulationOptions simulation, TimeProvider timeProvider)
        : base(DeviceKeys.IdScanner, "ID scanner (simulated)", isCritical: true, simulation, timeProvider)
    {
    }

    /// <inheritdoc />
    public async Task<IdScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfFaulted();
        await DelayAsync(1800, cancellationToken).ConfigureAwait(false);

        DateOnly today = DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime);
        var document = new IdDocument(
            FirstName: "Alex",
            LastName: "Sample",
            DateOfBirth: today.AddYears(-32),
            ExpiresOn: today.AddYears(4),
            DocumentNumber: "SIM-DL-00012345",
            IsGovernmentId: true,
            PortraitImage: SimulatedImages.OnePixelPng);

        return new IdScanResult(Succeeded: true, FailureReason: null, Document: document);
    }
}
