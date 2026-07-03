namespace GoldKiosk.Contracts.V1.Cloud.Items;

/// <summary>
/// Response body for <c>POST /api/v1/items/analyze</c> — the AI item-verification verdict.
/// AI-first, agent-escalation, never silently approved: a rejected or low-confidence
/// verdict sets <paramref name="RequiresLiveAgent"/> instead of auto-approving.
/// </summary>
/// <param name="Accepted">Whether the item passed AI verification.</param>
/// <param name="DetectedCategory">The category the AI detected, when identifiable.</param>
/// <param name="RejectReason">
/// The stable rejection reason code (see
/// <see cref="GoldKiosk.Contracts.V1.Common.RejectionReasonCodes"/>) when not accepted.
/// </param>
/// <param name="RequiresLiveAgent">Whether the verdict must be reviewed by a live agent.</param>
/// <param name="Confidence">The AI confidence in the verdict (0.0–1.0).</param>
public sealed record AnalyzeItemResponse(
    bool Accepted,
    string? DetectedCategory,
    string? RejectReason,
    bool RequiresLiveAgent,
    double Confidence);
