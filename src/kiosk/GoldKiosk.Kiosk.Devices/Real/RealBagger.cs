using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real bagger driver stub. Phase 4 target: arm-driven bagging sequence with the status
/// command / DAQ interlock (legacy <c>BAGGER_GET_STATUS</c> parity).
/// </summary>
public sealed class RealBagger : RealDeviceStub, IBagger
{
    /// <summary>Initializes the stub.</summary>
    public RealBagger()
        : base(DeviceKeys.Bagger, "Bagger (arm-driven)", isCritical: true)
    {
    }

    /// <inheritdoc />
    public Task<BaggerStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        throw NotWired();

    /// <inheritdoc />
    public Task BagItemAsync(string bagNumber, CancellationToken cancellationToken = default) =>
        throw NotWired();
}
