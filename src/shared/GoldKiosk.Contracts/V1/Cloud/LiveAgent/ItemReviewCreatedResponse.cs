namespace GoldKiosk.Contracts.V1.Cloud.LiveAgent;

/// <summary>
/// Response body for <c>POST /api/v1/live-agent/item-review</c> (replaces legacy
/// <c>UPLOAD-ITEM-IMAGE</c>) — the pending review the kiosk polls until an agent decides.
/// </summary>
/// <param name="ReviewId">The review identifier to poll.</param>
/// <param name="Status">The initial status; always <c>pending</c>.</param>
/// <param name="ExpiresAt">When the review times out (<c>timed_out</c> — never auto-approved).</param>
public sealed record ItemReviewCreatedResponse(
    Guid ReviewId,
    string Status,
    DateTimeOffset ExpiresAt);
