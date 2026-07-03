using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// Digital-input sensor board port. Real target: Advantech USB-4761 DAQ input bits
/// (tray, chamber, UPS-on-mains, chamber cup, scale cup).
/// </summary>
public interface ISensorBoard : IKioskDevice
{
    /// <summary>Reads the current state of all digital inputs.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>A snapshot of every sensor bit.</returns>
    Task<SensorSnapshot> ReadAsync(CancellationToken cancellationToken = default);
}
