namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>A single weight measurement from the scale.</summary>
/// <param name="Grams">Measured weight in grams.</param>
/// <param name="Stable"><see langword="true"/> when the reading had settled (stable indicator from the scale).</param>
public sealed record WeightReading(decimal Grams, bool Stable);
