using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Kiosk.Core.Pricing;

namespace GoldKiosk.Kiosk.Core.Cloud;

/// <summary>
/// The live-pricing <see cref="IOfferCalculator"/>: asks <see cref="ICloudGateway"/> for a
/// binding offer and, when the cloud is unreachable (or cannot fully express a pawn offer),
/// falls back to the injected local mock — marking the fallback offer <c>live_price = false</c>
/// so the UI can show that the price is not live (architecture-principles §4). The
/// orchestrator depends only on <see cref="IOfferCalculator"/>, so it is unchanged; the host
/// wraps the mock with this only when the cloud is enabled.
/// </summary>
public sealed class CloudBackedOfferCalculator : IOfferCalculator
{
    private const string GoldMetal = "gold";
    private const string PawnKind = "pawn";
    private const string SaleKind = "sale";

    private readonly ICloudGateway _gateway;
    private readonly IOfferCalculator _fallback;

    /// <summary>Initializes the calculator.</summary>
    /// <param name="gateway">The cloud gateway that computes the binding offer.</param>
    /// <param name="fallback">The local mock calculator used when the cloud is unavailable.</param>
    public CloudBackedOfferCalculator(ICloudGateway gateway, IOfferCalculator fallback)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
    }

    /// <inheritdoc />
    public async Task<OfferDto> CalculateAsync(OfferInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        bool isPawn = string.Equals(input.Kind, PawnKind, StringComparison.Ordinal);
        var cloudInput = new CloudOfferInput(input.WeightGrams, input.GoldPercent, GoldMetal, input.Kind);
        CloudOffer? cloud = await _gateway.GetOfferAsync(cloudInput, cancellationToken).ConfigureAwait(false);

        // Degrade to the local mock when the cloud is unreachable, or when it returned a
        // pawn offer without repayment terms (the wire contract cannot express those yet).
        if (cloud is null || (isPawn && cloud.PawnTerms is null))
        {
            OfferDto fallback = await _fallback.CalculateAsync(input, cancellationToken).ConfigureAwait(false);
            return fallback with { LivePrice = false };
        }

        return new OfferDto(
            OfferId: cloud.OfferId,
            Amount: new MoneyDto(cloud.AmountMinor, cloud.Currency, cloud.Display),
            Kind: isPawn ? PawnKind : SaleKind,
            Verified: true,
            LivePrice: true,
            ExpiresAt: cloud.ExpiresAt,
            PawnTerms: cloud.PawnTerms);
    }
}
