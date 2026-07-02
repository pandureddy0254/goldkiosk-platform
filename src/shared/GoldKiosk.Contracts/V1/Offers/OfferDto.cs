using GoldKiosk.Contracts.V1.Common;

namespace GoldKiosk.Contracts.V1.Offers;

/// <summary>
/// An offer made to the customer. No analysis internals (weight, karat, percentages) are exposed.
/// </summary>
/// <param name="OfferId">The offer identifier, e.g. <c>off_01JZC…</c>.</param>
/// <param name="Amount">The offered amount.</param>
/// <param name="Kind">The offer kind: <c>sale</c> or <c>pawn</c>.</param>
/// <param name="Verified">Whether the item passed authenticity verification.</param>
/// <param name="LivePrice">Whether the offer was priced against the live market feed.</param>
/// <param name="ExpiresAt">When the offer's price lock expires (drives the countdown chip).</param>
/// <param name="PawnTerms">The repayment terms; present only when <paramref name="Kind"/> is <c>pawn</c>.</param>
public sealed record OfferDto(
    string OfferId,
    MoneyDto Amount,
    string Kind,
    bool Verified,
    bool LivePrice,
    DateTimeOffset ExpiresAt,
    PawnTermsDto? PawnTerms);
