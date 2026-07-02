namespace GoldKiosk.Contracts.V1.Events;

/// <summary>
/// SignalR <c>agent_status</c> payload — the live-agent review status changed.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="Sequence">The per-session monotonic event sequence.</param>
/// <param name="Status">The review status: <c>connecting</c>, <c>agent_joined</c>, <c>approved</c>, <c>declined</c> or <c>unavailable</c>.</param>
/// <param name="AgentRef">The reviewing agent reference (e.g. <c>agt_204</c>), once an agent joined.</param>
public sealed record AgentStatusEvent(string SessionId, long Sequence, string Status, string? AgentRef);
