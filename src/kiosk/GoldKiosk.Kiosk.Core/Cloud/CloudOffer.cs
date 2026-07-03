using GoldKiosk.Contracts.V1.Offers;

namespace GoldKiosk.Kiosk.Core.Cloud;

/// <summary>
/// A binding offer returned by the cloud, carried across <see cref="ICloudGateway"/> in
/// framework-free terms. The amount stays as minor units + currency + a preformatted
/// display string so the edge never re-derives money.
/// </summary>
/// <param name="AmountMinor">The offered amount in the currency's minor units.</param>
/// <param name="Currency">The ISO 4217 currency code.</param>
/// <param name="Display">The preformatted display string, e.g. <c>$8,460.00</c>.</param>
/// <param name="OfferId">The server-issued quote id, as a string (grounds the offer explainer).</param>
/// <param name="ExpiresAt">When the offer's price lock expires.</param>
/// <param name="PawnTerms">
/// The repayment terms for a pawn offer, when the cloud supplies them; <see langword="null"/>
/// for a sale, or for a pawn the wire contract cannot yet express.
/// </param>
public sealed record CloudOffer(
    long AmountMinor,
    string Currency,
    string Display,
    string OfferId,
    DateTimeOffset ExpiresAt,
    PawnTermsDto? PawnTerms);
