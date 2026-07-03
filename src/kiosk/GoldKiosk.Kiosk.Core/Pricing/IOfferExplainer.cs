using GoldKiosk.Contracts.V1.Offers;

namespace GoldKiosk.Kiosk.Core.Pricing;

/// <summary>
/// Port for the "How was this calculated?" offer explainer. Data minimization is binding:
/// implementations receive only the formatted offer (never analysis internals or PII) —
/// the AI-backed implementation arrives with Cloud.Api.
/// </summary>
public interface IOfferExplainer
{
    /// <summary>Produces the customer-facing explanation for an offer.</summary>
    /// <param name="offer">The offer being explained.</param>
    /// <param name="question">The customer's optional question.</param>
    /// <param name="cancellationToken">Cancels the explanation.</param>
    /// <returns>The explanation and its disclaimer.</returns>
    Task<ExplainOfferResponse> ExplainAsync(
        OfferDto offer,
        string? question,
        CancellationToken cancellationToken = default);
}
