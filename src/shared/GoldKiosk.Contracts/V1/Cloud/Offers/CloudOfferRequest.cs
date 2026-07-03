namespace GoldKiosk.Contracts.V1.Cloud.Offers;

/// <summary>
/// Request body for <c>POST /api/v1/offers</c> (replaces legacy <c>GET-OFFER</c>): the
/// kiosk's measured item facts from which the cloud computes the binding offer.
/// </summary>
/// <param name="Metal">The assayed metal, e.g. <c>gold</c>, <c>silver</c>.</param>
/// <param name="Karat">The assayed karat (24 = fine gold).</param>
/// <param name="PurityPercent">The assayed purity in percent (0–100).</param>
/// <param name="WeightGrams">The measured weight in grams.</param>
/// <param name="Category">The item category key, e.g. <c>ring</c>.</param>
/// <param name="Kind">The transaction kind: <c>sale</c> or <c>pawn</c>.</param>
public sealed record CloudOfferRequest(
    string Metal,
    decimal Karat,
    decimal PurityPercent,
    decimal WeightGrams,
    string Category,
    string Kind);
