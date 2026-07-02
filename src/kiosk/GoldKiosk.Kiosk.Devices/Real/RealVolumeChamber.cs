using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real volume chamber driver stub. Phase 4 target hardware: pressure sensor on COM4
/// (115200) + stepper board on COM3 (9600, <c>cu/cd/pu/pd</c> commands), porting the legacy
/// Boyle's-law two-pressure math with reconciled chamber constants.
/// </summary>
public sealed class RealVolumeChamber : RealDeviceStub, IVolumeChamber
{
    /// <summary>Initializes the stub.</summary>
    public RealVolumeChamber()
        : base(DeviceKeys.VolumeChamber, "Volume chamber (pressure COM4 + stepper COM3)", isCritical: true)
    {
    }

    /// <inheritdoc />
    public Task<VolumeReading> MeasureAsync(decimal weightGrams, CancellationToken cancellationToken = default) =>
        throw NotWired();
}
