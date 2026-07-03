namespace GoldKiosk.Contracts.V1.Cloud.LiveAgent;

/// <summary>
/// Request body for <c>POST /api/v1/live-agent/availability</c> (replaces legacy
/// <c>UPDATE-WORKING</c>) — reports the agent desk's on-duty state.
/// </summary>
/// <param name="IsAvailable">Whether at least one agent is on duty.</param>
/// <param name="AgentsOnline">The number of agents on duty; defaults to 1 when available.</param>
public sealed record SetAgentAvailabilityRequest(bool IsAvailable, int? AgentsOnline);
