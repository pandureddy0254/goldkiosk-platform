namespace GoldKiosk.Cloud.Api.Services.LiveAgent;

/// <summary>
/// A live-agent item review. Verdicts are written only by agents (AdminPortal surface);
/// TTL expiry produces <c>timed_out</c> — an auto-approve path does not exist by design.
/// </summary>
/// <param name="ReviewId">The review id.</param>
/// <param name="KioskId">The escalating kiosk.</param>
/// <param name="TenantId">The kiosk's tenant.</param>
/// <param name="TransactionRef">The kiosk transaction reference the review belongs to.</param>
/// <param name="CreatedAt">When the review was created.</param>
/// <param name="ExpiresAt">When the review times out without a verdict.</param>
/// <param name="Status">The status: <c>pending</c>, <c>approved</c>, <c>rejected</c>, <c>timed_out</c>.</param>
/// <param name="Reason">The agent's reason code when rejected.</param>
public sealed record ItemReview(
    Guid ReviewId,
    Guid KioskId,
    Guid TenantId,
    string TransactionRef,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    string Status,
    string? Reason)
{
    /// <summary>The pending status value.</summary>
    public const string StatusPending = "pending";

    /// <summary>The approved status value (agent verdict only).</summary>
    public const string StatusApproved = "approved";

    /// <summary>The rejected status value (agent verdict only).</summary>
    public const string StatusRejected = "rejected";

    /// <summary>The timed-out status value (TTL expiry; the kiosk returns the item).</summary>
    public const string StatusTimedOut = "timed_out";
}
