namespace GoldKiosk.Contracts.V1.Offers;

/// <summary>
/// Response body for <c>POST /api/v1/sessions/{id}/offer/explain</c>.
/// </summary>
/// <param name="Explanation">The customer-facing explanation of how the offer was calculated.</param>
/// <param name="Disclaimer">The validity/market-movement disclaimer shown with the explanation.</param>
public sealed record ExplainOfferResponse(string Explanation, string Disclaimer);
