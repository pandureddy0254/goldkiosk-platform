using GoldKiosk.Contracts.V1.Common;

namespace GoldKiosk.Contracts.V1.Cloud.Offers;

/// <summary>
/// Response body for <c>POST /api/v1/offers</c> — the computed offer with its price lock.
/// Pricing internals (rate, margin) stay server-side; the kiosk shows only the amount.
/// </summary>
/// <param name="OfferId">The server-issued quote identifier (grounds <c>offers/explain</c>).</param>
/// <param name="Amount">The offered amount, floored house-favorably to the rounding step.</param>
/// <param name="Summary">A one-line item summary, e.g. <c>1 Pc 22K gold ring · 12.40 g</c>.</param>
/// <param name="RateAsOf">When the market rate used for this offer was sourced.</param>
/// <param name="LockedUntil">When the offer's price lock expires.</param>
public sealed record CloudOfferResponse(
    Guid OfferId,
    MoneyDto Amount,
    string Summary,
    DateTimeOffset RateAsOf,
    DateTimeOffset LockedUntil);
