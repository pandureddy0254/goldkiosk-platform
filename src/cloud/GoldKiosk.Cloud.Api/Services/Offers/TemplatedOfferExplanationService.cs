using System.Globalization;
using GoldKiosk.Contracts.V1.Cloud.Offers;

namespace GoldKiosk.Cloud.Api.Services.Offers;

/// <summary>
/// Deterministic, grounded offer explanation — templated text built strictly from the
/// stored quote (or the formatted amount the kiosk sent), never a language model. Keeps
/// the live-market framing of the platform2 explainer with zero fabrication risk and full
/// data minimization: only the amount and an optional item one-liner are ever involved.
/// Localization-ready: templates are selected by locale; English ships first.
/// </summary>
/// <param name="quoteStore">The quote store for grounding by offer id.</param>
/// <param name="timeProvider">The clock (drives the lock-remaining framing).</param>
public sealed class TemplatedOfferExplanationService(
    OfferQuoteStore quoteStore,
    TimeProvider timeProvider) : IOfferExplanationService
{
    /// <inheritdoc />
    public CloudExplainOfferResponse Explain(CloudExplainOfferRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        OfferQuote? quote = request.OfferId is Guid offerId ? quoteStore.Get(offerId) : null;

        // Locale seam: only "en" templates exist today; unknown locales fall back to English.
        return quote is not null
            ? ExplainFromQuote(quote, request.Question)
            : ExplainFromAmount(request.AmountDisplay, request.ItemSummary, request.Question);
    }

    private CloudExplainOfferResponse ExplainFromQuote(OfferQuote quote, string? question)
    {
        int lockMinutes = Math.Max(
            0, (int)Math.Ceiling((quote.LockedUntil - timeProvider.GetUtcNow()).TotalMinutes));

        string explanation = string.Create(
            CultureInfo.InvariantCulture,
            $"Your offer of {quote.Amount} for {quote.Summary} starts from the live " +
            $"{quote.Metal} market price as of {quote.RateAsOf:HH:mm 'UTC on' d MMM yyyy}. " +
            $"We valued the precious-metal content of your item at that market rate, " +
            $"applied our service margin, and rounded the result down to a clean figure. " +
            $"The market moves continuously, so this price is locked in for you right now.");

        if (!string.IsNullOrWhiteSpace(question))
        {
            explanation += " Every offer follows this same calculation — the live market "
                + "rate at the moment of analysis, our published margin, and nothing else. "
                + "A team member can walk through the details via the help option.";
        }

        string disclaimer = lockMinutes > 0
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"This offer is locked for about {lockMinutes} more minute(s). After it expires, " +
                $"a fresh analysis at the then-current market price may produce a different amount.")
            : "This offer's price lock has expired. A fresh analysis at the current market "
                + "price may produce a different amount.";

        return new CloudExplainOfferResponse(explanation, disclaimer);
    }

    private static CloudExplainOfferResponse ExplainFromAmount(
        string? amountDisplay, string? itemSummary, string? question)
    {
        string amountPart = string.IsNullOrWhiteSpace(amountDisplay)
            ? "Your offer"
            : $"Your offer of {amountDisplay}";
        string itemPart = string.IsNullOrWhiteSpace(itemSummary) ? "your item" : itemSummary;

        string explanation =
            $"{amountPart} reflects the live precious-metals market price at the moment " +
            $"{itemPart} was analyzed. We measured the metal content, valued it at the " +
            "current market rate, applied our service margin, and rounded the result down " +
            "to a clean figure.";

        if (!string.IsNullOrWhiteSpace(question))
        {
            explanation += " Every offer follows this same calculation — the live market "
                + "rate at the moment of analysis, our published margin, and nothing else. "
                + "A team member can walk through the details via the help option.";
        }

        const string disclaimer =
            "Market prices move continuously. This offer is valid only while its price "
            + "lock is active; a new analysis may produce a different amount.";

        return new CloudExplainOfferResponse(explanation, disclaimer);
    }
}
