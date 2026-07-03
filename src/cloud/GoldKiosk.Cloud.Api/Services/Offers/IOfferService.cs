using GoldKiosk.Contracts.V1.Cloud.Offers;

namespace GoldKiosk.Cloud.Api.Services.Offers;

/// <summary>
/// Computes customer offers from measured item facts and live market rates
/// (replaces legacy <c>GET-OFFER</c>).
/// </summary>
public interface IOfferService
{
    /// <summary>Computes an offer for the measured item.</summary>
    /// <param name="request">The kiosk's measured item facts.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The offer, or <see langword="null"/> when no usable market rate exists.</returns>
    Task<CloudOfferResponse?> ComputeOfferAsync(
        CloudOfferRequest request, CancellationToken cancellationToken = default);
}
