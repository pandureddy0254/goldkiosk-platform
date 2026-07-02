using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real ID scanner driver stub. Phase 4 target SDKs behind this one port: Acuant ScanShell
/// (in-process) and the 3M/Gemalto full-page reader (visible/IR/UV); the composition root
/// selects the driver for the fitted scanner.
/// </summary>
public sealed class RealIdScanner : RealDeviceStub, IIdScanner
{
    /// <summary>Initializes the stub.</summary>
    public RealIdScanner()
        : base(DeviceKeys.IdScanner, "ID scanner (Acuant / Gemalto)", isCritical: true)
    {
    }

    /// <inheritdoc />
    public Task<IdScanResult> ScanAsync(CancellationToken cancellationToken = default) =>
        throw NotWired();
}
