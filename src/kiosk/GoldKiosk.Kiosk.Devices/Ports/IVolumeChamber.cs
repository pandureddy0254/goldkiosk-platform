using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// Volume measurement chamber port (density cross-check against the XRF reading). Real target:
/// pressure sensor on COM4 (115200) + stepper board on COM3 (9600, <c>cu/cd/pu/pd</c>
/// commands) using the legacy Boyle's-law two-pressure math with reconciled chamber constants.
/// </summary>
public interface IVolumeChamber : IKioskDevice
{
    /// <summary>Measures the volume of the item currently in the chamber.</summary>
    /// <param name="weightGrams">The item weight from the scale, used in the density computation.</param>
    /// <param name="cancellationToken">Cancels the measurement.</param>
    /// <returns>The volume reading, including chamber calibration status.</returns>
    Task<VolumeReading> MeasureAsync(decimal weightGrams, CancellationToken cancellationToken = default);
}
