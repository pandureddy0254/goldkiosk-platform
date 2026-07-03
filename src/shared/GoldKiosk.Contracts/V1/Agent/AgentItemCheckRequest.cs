namespace GoldKiosk.Contracts.V1.Agent;

/// <summary>
/// Request body for <c>POST /api/v1/sessions/{id}/agent/item-check</c> — live-agent
/// escalation. A low-confidence AI verdict escalates; it is never silently approved.
/// </summary>
/// <param name="Trigger">What triggered the escalation, e.g. <c>analysis_escalation</c>.</param>
public sealed record AgentItemCheckRequest(string Trigger);
