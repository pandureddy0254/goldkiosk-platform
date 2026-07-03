namespace GoldKiosk.Contracts.V1.Events;

/// <summary>
/// SignalR <c>session_aborted</c> payload — the session was aborted.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="Sequence">The per-session monotonic event sequence.</param>
/// <param name="Reason">The abort reason: <c>timeout</c>, <c>user_cancel</c>, <c>operator</c> or <c>fault</c>.</param>
public sealed record SessionAbortedEvent(string SessionId, long Sequence, string Reason);
