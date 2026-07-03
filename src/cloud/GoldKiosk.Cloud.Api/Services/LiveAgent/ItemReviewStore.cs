using System.Collections.Concurrent;
using GoldKiosk.Cloud.Api.Logging;
using GoldKiosk.Cloud.Api.Options;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.Api.Services.LiveAgent;

/// <summary>
/// In-memory pending-review store for live-agent item checks.
/// <para>
/// TODO(GK-LA-1): move to a durable review table (with tray-image Blob reference and
/// SignalR agent notification) — the schema has no suitable entity yet; in-memory keeps
/// the fail-closed semantics intact for the kiosk vertical. Restarting the host loses
/// pending reviews, which resolves as <c>timed_out</c> on the kiosk — the safe outcome.
/// </para>
/// </summary>
/// <param name="liveAgentOptions">The review TTL policy.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The host logger.</param>
public sealed class ItemReviewStore(
    IOptions<LiveAgentOptions> liveAgentOptions,
    TimeProvider timeProvider,
    ILogger<ItemReviewStore> logger)
{
    private readonly ConcurrentDictionary<Guid, ItemReview> _reviews = new();

    /// <summary>Creates a pending review.</summary>
    /// <param name="kioskId">The escalating kiosk.</param>
    /// <param name="tenantId">The kiosk's tenant.</param>
    /// <param name="transactionRef">The kiosk transaction reference.</param>
    /// <returns>The created review.</returns>
    public ItemReview Create(Guid kioskId, Guid tenantId, string transactionRef)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionRef);

        DateTimeOffset now = timeProvider.GetUtcNow();
        var review = new ItemReview(
            ReviewId: Guid.NewGuid(),
            KioskId: kioskId,
            TenantId: tenantId,
            TransactionRef: transactionRef,
            CreatedAt: now,
            ExpiresAt: now.AddSeconds(liveAgentOptions.Value.ReviewTtlSeconds),
            Status: ItemReview.StatusPending,
            Reason: null);

        _reviews[review.ReviewId] = review;
        logger.ReviewCreated(review.ReviewId, transactionRef);
        return review;
    }

    /// <summary>
    /// Gets a review, applying TTL: a pending review past its expiry becomes
    /// <c>timed_out</c> — never approved.
    /// </summary>
    /// <param name="reviewId">The review id.</param>
    /// <returns>The review, or <see langword="null"/> when unknown.</returns>
    public ItemReview? Get(Guid reviewId)
    {
        if (!_reviews.TryGetValue(reviewId, out ItemReview? review))
        {
            return null;
        }

        if (review.Status == ItemReview.StatusPending && timeProvider.GetUtcNow() >= review.ExpiresAt)
        {
            ItemReview timedOut = review with { Status = ItemReview.StatusTimedOut };
            if (_reviews.TryUpdate(reviewId, timedOut, review))
            {
                logger.ReviewTimedOut(reviewId);
            }

            return _reviews.GetValueOrDefault(reviewId, timedOut);
        }

        return review;
    }

    /// <summary>
    /// Records an agent verdict on a pending review. Called by the agent surface
    /// (AdminPortal) — never by kiosk-facing endpoints.
    /// </summary>
    /// <param name="reviewId">The review id.</param>
    /// <param name="approved">The verdict.</param>
    /// <param name="reason">The reason code when rejected.</param>
    /// <returns><see langword="true"/> when a pending review was updated.</returns>
    public bool TrySetVerdict(Guid reviewId, bool approved, string? reason)
    {
        if (!_reviews.TryGetValue(reviewId, out ItemReview? review)
            || review.Status != ItemReview.StatusPending
            || timeProvider.GetUtcNow() >= review.ExpiresAt)
        {
            return false;
        }

        ItemReview decided = review with
        {
            Status = approved ? ItemReview.StatusApproved : ItemReview.StatusRejected,
            Reason = approved ? null : reason,
        };
        return _reviews.TryUpdate(reviewId, decided, review);
    }
}
