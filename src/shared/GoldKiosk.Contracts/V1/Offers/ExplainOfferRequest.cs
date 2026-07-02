namespace GoldKiosk.Contracts.V1.Offers;

/// <summary>
/// Request body for <c>POST /api/v1/sessions/{id}/offer/explain</c> — the AI
/// "How was this calculated?" explainer.
/// </summary>
/// <param name="Question">An optional customer question; omitted for the default explanation.</param>
public sealed record ExplainOfferRequest(string? Question);
