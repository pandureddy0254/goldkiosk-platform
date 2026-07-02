using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real fingerprint scanner driver stub. Phase 4 target SDK: FlexCode SDK (serial +
/// activation licensing; licence keys arrive via the config layer, never baked in).
/// </summary>
public sealed class RealFingerprintScanner : RealDeviceStub, IFingerprintScanner
{
    /// <summary>Initializes the stub.</summary>
    public RealFingerprintScanner()
        : base(DeviceKeys.FingerprintScanner, "Fingerprint scanner (FlexCode)", isCritical: false)
    {
    }

    /// <inheritdoc />
    public Task<FingerprintResult> CaptureAsync(CancellationToken cancellationToken = default) =>
        throw NotWired();
}
