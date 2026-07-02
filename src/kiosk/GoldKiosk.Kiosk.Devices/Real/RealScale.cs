using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real scale driver stub. Phase 4 target SDK: MT-SICS-style ASCII protocol over RS-232
/// COM5 (9600 8-N-1, <c>Q</c>/<c>Z</c> commands) with a robust parse and settle discipline.
/// </summary>
public sealed class RealScale : RealDeviceStub, IScale
{
    /// <summary>Initializes the stub.</summary>
    public RealScale()
        : base(DeviceKeys.Scale, "Precision scale (MT-SICS, COM5)", isCritical: true)
    {
    }

    /// <inheritdoc />
    public Task<WeightReading> GetWeightAsync(CancellationToken cancellationToken = default) =>
        throw NotWired();

    /// <inheritdoc />
    public Task ZeroAsync(CancellationToken cancellationToken = default) =>
        throw NotWired();
}
