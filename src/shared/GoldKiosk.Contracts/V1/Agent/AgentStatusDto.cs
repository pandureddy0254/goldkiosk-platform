namespace GoldKiosk.Contracts.V1.Agent;

/// <summary>
/// The live-agent review status. <c>unavailable</c> falls back to policy (AI verdict stands
/// or the session aborts) — a fabricated approval never exists.
/// </summary>
/// <param name="Status">The review status: <c>connecting</c>, <c>agent_joined</c>, <c>approved</c>, <c>declined</c> or <c>unavailable</c>.</param>
/// <param name="AgentRef">The reviewing agent reference (e.g. <c>agt_204</c>), once an agent joined.</param>
public sealed record AgentStatusDto(string Status, string? AgentRef);
