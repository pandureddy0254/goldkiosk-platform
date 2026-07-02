using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real robotic arm driver stub. Phase 4 target SDKs: Dobot over TCP (dashboard :29999,
/// motion :30003/:30004) plus the Kollmorgen AKD linear axis (telnet ASCII,
/// <c>drv.en</c>/<c>mt.move</c>/<c>motionstat</c> polling) with motion-complete confirmation.
/// </summary>
public sealed class RealRoboticArm : RealDeviceStub, IRoboticArm
{
    /// <summary>Initializes the stub.</summary>
    public RealRoboticArm()
        : base(DeviceKeys.RoboticArm, "Robotic arm (Dobot TCP + AKD axis)", isCritical: true)
    {
    }

    /// <inheritdoc />
    public Task MoveAsync(ArmMove move, CancellationToken cancellationToken = default) =>
        throw NotWired();
}
