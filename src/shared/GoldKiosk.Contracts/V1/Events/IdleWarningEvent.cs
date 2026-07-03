namespace GoldKiosk.Contracts.V1.Events;

/// <summary>
/// SignalR <c>idle_warning</c> payload — drives the idle-timeout countdown ring overlay.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="Sequence">The per-session monotonic event sequence.</param>
/// <param name="SecondsRemaining">Seconds until the session auto-aborts for inactivity.</param>
public sealed record IdleWarningEvent(string SessionId, long Sequence, int SecondsRemaining);
