namespace GoldKiosk.Contracts.V1.Tray;

/// <summary>
/// The tray hardware status on the wire.
/// </summary>
/// <param name="Status">The tray status, e.g. <c>opening</c>, <c>open</c>, <c>closing</c>, <c>closed</c>.</param>
public sealed record TrayDto(string Status);
