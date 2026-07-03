using GoldKiosk.Contracts.V1.Offers;

namespace GoldKiosk.Kiosk.Core.Pricing;

/// <summary>
/// Canned, grounded offer explanation used until the AI explainer arrives with Cloud.Api.
/// Grounded means it references only the actual offer on the table (amount + kind) —
/// never analysis internals.
/// </summary>
public sealed class MockOfferExplainer : IOfferExplainer
{
    private const string Disclaimer =
        "Offers are valid while the timer is running and may change with the live market.";

    /// <inheritdoc />
    public Task<ExplainOfferResponse> ExplainAsync(
        OfferDto offer,
        string? question,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(offer);
        cancellationToken.ThrowIfCancellationRequested();

        string explanation = string.Equals(offer.Kind, "pawn", StringComparison.Ordinal)
            ? $"Your pawn offer of {offer.Amount.Display} reflects today's live market price for your "
              + "item's verified precious-metal content, minus our margin. Repayment terms, including "
              + "the monthly fee and due date, are shown alongside the offer."
            : $"Your offer of {offer.Amount.Display} reflects today's live market price for your item's "
              + "verified precious-metal content, minus our margin. Market spot prices quote pure refined "
              + "metal; jewellery contains alloys and the offer accounts for refining.";

        return Task.FromResult(new ExplainOfferResponse(explanation, Disclaimer));
    }
}
