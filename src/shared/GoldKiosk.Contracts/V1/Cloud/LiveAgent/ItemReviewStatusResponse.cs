namespace GoldKiosk.Contracts.V1.Cloud.LiveAgent;

/// <summary>
/// Response body for <c>GET /api/v1/live-agent/item-review/{id}</c> (replaces legacy
/// <c>ITEM-APPROVAL-STATUS</c>). The status only moves to <c>approved</c>/<c>rejected</c>
/// by an agent verdict written from the AdminPortal surface; TTL expiry yields
/// <c>timed_out</c>. A forced/fabricated approval path does not exist.
/// </summary>
/// <param name="ReviewId">The review identifier.</param>
/// <param name="Status">The status: <c>pending</c>, <c>approved</c>, <c>rejected</c> or <c>timed_out</c>.</param>
/// <param name="Reason">The agent's reason code, present when rejected.</param>
public sealed record ItemReviewStatusResponse(
    Guid ReviewId,
    string Status,
    string? Reason);
