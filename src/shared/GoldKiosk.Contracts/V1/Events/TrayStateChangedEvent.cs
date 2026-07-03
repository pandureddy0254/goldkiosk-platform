using GoldKiosk.Contracts.V1.Tray;

namespace GoldKiosk.Contracts.V1.Events;

/// <summary>
/// SignalR <c>tray_state_changed</c> payload — the tray hardware moved.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="Sequence">The per-session monotonic event sequence.</param>
/// <param name="Tray">The tray movement status.</param>
public sealed record TrayStateChangedEvent(string SessionId, long Sequence, TrayDto Tray);
