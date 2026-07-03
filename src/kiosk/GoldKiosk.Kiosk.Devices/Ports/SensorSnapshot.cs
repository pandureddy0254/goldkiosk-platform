namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>A point-in-time snapshot of the machine's digital sensor bits.</summary>
/// <param name="TrayClosed"><see langword="true"/> when the customer tray is fully closed.</param>
/// <param name="ChamberClosed"><see langword="true"/> when the volume chamber is sealed.</param>
/// <param name="UpsOnMains"><see langword="true"/> when the UPS reports mains power (not on battery).</param>
/// <param name="ChamberCupPresent"><see langword="true"/> when the chamber measurement cup is seated.</param>
/// <param name="ScaleCupPresent"><see langword="true"/> when the scale cup is seated.</param>
public sealed record SensorSnapshot(
    bool TrayClosed,
    bool ChamberClosed,
    bool UpsOnMains,
    bool ChamberCupPresent,
    bool ScaleCupPresent);
