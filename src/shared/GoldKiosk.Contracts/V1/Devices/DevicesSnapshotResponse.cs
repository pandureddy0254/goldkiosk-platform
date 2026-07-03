namespace GoldKiosk.Contracts.V1.Devices;

/// <summary>
/// Response body for <c>GET /api/v1/devices</c> — the fleet-health snapshot of every
/// hardware port on this kiosk.
/// </summary>
/// <param name="Overall">The aggregate health, e.g. <c>healthy</c>, <c>degraded</c>, <c>faulted</c>.</param>
/// <param name="Devices">The per-device statuses.</param>
public sealed record DevicesSnapshotResponse(string Overall, IReadOnlyList<DeviceStatusDto> Devices);
