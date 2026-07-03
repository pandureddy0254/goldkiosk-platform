namespace GoldKiosk.Contracts.V1.Cloud.Offers;

/// <summary>
/// Request body for <c>POST /api/v1/offers/explain</c> — the grounded "How was this
/// calculated?" explainer. Data-minimized by design: only the offer reference (or a
/// formatted amount) and an optional item one-liner ever leave the kiosk.
/// </summary>
/// <param name="OfferId">The quote id from <c>POST /api/v1/offers</c>; preferred grounding.</param>
/// <param name="AmountDisplay">Fallback grounding: the formatted offer amount, e.g. <c>₹42,180</c>.</param>
/// <param name="ItemSummary">An optional item one-liner, e.g. <c>1 Pc 22K gold ring</c>.</param>
/// <param name="Question">An optional customer question; omitted for the default explanation.</param>
/// <param name="Locale">The BCP-47 display locale, e.g. <c>en</c>; defaults to English.</param>
public sealed record CloudExplainOfferRequest(
    Guid? OfferId,
    string? AmountDisplay,
    string? ItemSummary,
    string? Question,
    string? Locale);
