namespace GoldKiosk.Contracts.V1.Cloud.LiveAgent;

/// <summary>
/// The live-agent desk availability (replaces legacy <c>GETWORKING</c>).
/// </summary>
/// <param name="IsAvailable">Whether at least one agent is on duty.</param>
/// <param name="AgentsOnline">The number of agents currently on duty.</param>
/// <param name="UpdatedAt">When availability was last reported.</param>
public sealed record AgentAvailabilityDto(
    bool IsAvailable,
    int AgentsOnline,
    DateTimeOffset UpdatedAt);
