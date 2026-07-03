namespace GoldKiosk.Contracts.V1.Events;

/// <summary>
/// SignalR <c>device_health_changed</c> payload — the kiosk's aggregate hardware health changed.
/// </summary>
/// <param name="SessionId">The session identifier the event was delivered under.</param>
/// <param name="Sequence">The per-session monotonic event sequence.</param>
/// <param name="Overall">The aggregate health, e.g. <c>healthy</c>, <c>degraded</c>, <c>faulted</c>.</param>
public sealed record DeviceHealthChangedEvent(string SessionId, long Sequence, string Overall);
