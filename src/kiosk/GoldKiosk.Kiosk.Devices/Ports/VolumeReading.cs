namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>A volume measurement from the chamber.</summary>
/// <param name="VolumeCc">Measured volume in cubic centimetres.</param>
/// <param name="Calibrated"><see langword="false"/> when the chamber constants are stale and the reading is advisory only.</param>
public sealed record VolumeReading(decimal VolumeCc, bool Calibrated);
