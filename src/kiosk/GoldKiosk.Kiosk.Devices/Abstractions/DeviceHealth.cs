namespace GoldKiosk.Kiosk.Devices.Abstractions;

/// <summary>A point-in-time health snapshot of a kiosk device.</summary>
/// <param name="State">The current lifecycle state.</param>
/// <param name="Detail">Optional human-readable detail (fault reason, connection note). Never PII.</param>
public sealed record DeviceHealth(DeviceState State, string? Detail = null);
