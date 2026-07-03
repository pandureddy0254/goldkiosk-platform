using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// Bag label printer port. Real target: Brother b-PAC with an <c>.lbx</c> template; fields
/// mirror the legacy label (invoice, bag number, weight, offer display).
/// </summary>
public interface ILabelPrinter : IKioskDevice
{
    /// <summary>Prints one bag label.</summary>
    /// <param name="label">The label field values.</param>
    /// <param name="cancellationToken">Cancels waiting for print completion.</param>
    /// <returns>A task that completes when the label has been printed.</returns>
    Task PrintAsync(BagLabel label, CancellationToken cancellationToken = default);
}
