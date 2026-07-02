using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real label printer driver stub. Phase 4 target SDK: Brother b-PAC printing an
/// <c>.lbx</c> template (invoice, bag number, weight, offer display fields).
/// </summary>
public sealed class RealLabelPrinter : RealDeviceStub, ILabelPrinter
{
    /// <summary>Initializes the stub.</summary>
    public RealLabelPrinter()
        : base(DeviceKeys.LabelPrinter, "Label printer (Brother b-PAC)", isCritical: false)
    {
    }

    /// <inheritdoc />
    public Task PrintAsync(BagLabel label, CancellationToken cancellationToken = default) =>
        throw NotWired();
}
