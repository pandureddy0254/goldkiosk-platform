using GoldKiosk.Contracts.V1.Offers;

namespace GoldKiosk.Kiosk.Core.Pricing;

/// <summary>
/// Port for offer pricing. The mock implementation prices against the configured rate
/// table; live cloud pricing (Cloud.Api) replaces the implementation behind this interface
/// without touching the flow.
/// </summary>
public interface IOfferCalculator
{
    /// <summary>Calculates the offer for an analysed item, including the price-lock expiry.</summary>
    /// <param name="input">The pricing inputs.</param>
    /// <param name="cancellationToken">Cancels the calculation.</param>
    /// <returns>The offer ready for the wire.</returns>
    Task<OfferDto> CalculateAsync(OfferInput input, CancellationToken cancellationToken = default);
}
