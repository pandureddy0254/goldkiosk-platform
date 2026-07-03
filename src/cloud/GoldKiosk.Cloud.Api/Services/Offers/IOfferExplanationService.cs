using GoldKiosk.Contracts.V1.Cloud.Offers;

namespace GoldKiosk.Cloud.Api.Services.Offers;

/// <summary>
/// Produces the customer-facing "How was this calculated?" explanation for an offer.
/// </summary>
public interface IOfferExplanationService
{
    /// <summary>Builds the explanation for an offer.</summary>
    /// <param name="request">The explain request (offer reference or formatted amount).</param>
    /// <returns>The explanation and disclaimer.</returns>
    CloudExplainOfferResponse Explain(CloudExplainOfferRequest request);
}
