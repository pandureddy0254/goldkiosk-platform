using GoldKiosk.Contracts.V1.Cloud.LiveAgent;

namespace GoldKiosk.Cloud.Api.Services.LiveAgent;

/// <summary>
/// In-memory live-agent desk availability (replaces legacy <c>GETWORKING</c> /
/// <c>UPDATE-WORKING</c>). Defaults to unavailable — a kiosk that cannot reach an agent
/// follows the fail-closed path (timeout → return item), never a fabricated approval.
/// TODO(GK-LA-1): persist alongside the review table when the agent surface lands.
/// </summary>
/// <param name="timeProvider">The clock.</param>
public sealed class AgentAvailabilityStore(TimeProvider timeProvider)
{
    private readonly Lock _gate = new();
    private bool _isAvailable;
    private int _agentsOnline;
    private DateTimeOffset _updatedAt = DateTimeOffset.MinValue;

    /// <summary>Gets the current availability snapshot.</summary>
    /// <returns>The availability DTO.</returns>
    public AgentAvailabilityDto Get()
    {
        lock (_gate)
        {
            return new AgentAvailabilityDto(_isAvailable, _agentsOnline, _updatedAt);
        }
    }

    /// <summary>Updates the availability.</summary>
    /// <param name="isAvailable">Whether at least one agent is on duty.</param>
    /// <param name="agentsOnline">The agent count; defaults to 1 when available, 0 otherwise.</param>
    /// <returns>The updated snapshot.</returns>
    public AgentAvailabilityDto Set(bool isAvailable, int? agentsOnline)
    {
        lock (_gate)
        {
            _isAvailable = isAvailable;
            _agentsOnline = agentsOnline ?? (isAvailable ? 1 : 0);
            _updatedAt = timeProvider.GetUtcNow();
            return new AgentAvailabilityDto(_isAvailable, _agentsOnline, _updatedAt);
        }
    }
}
