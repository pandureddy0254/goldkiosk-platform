namespace GoldKiosk.Cloud.Api.Services.Offers;

/// <summary>
/// In-memory, bounded store of recent offer quotes (grounding for
/// <c>offers/explain</c> within the lock window).
/// <para>
/// Quote-time persistence has no schema home yet: <c>pricing.offers</c> requires
/// NOT NULL FKs to <c>tx.transactions</c> and <c>pricing.pricing_policies</c>, neither
/// of which exists when a quote is computed (the transaction row is created at
/// settlement). TODO(GK-OFFER-1): persist quotes once a quote table or draft-transaction
/// flow lands; until then quotes are process-local and expire with the lock.
/// </para>
/// </summary>
public sealed class OfferQuoteStore
{
    // A kiosk fleet produces a handful of quotes per minute; 1000 entries comfortably
    // covers every active lock window while bounding memory. Oldest evicted first.
    private const int MaxEntries = 1000;

    private readonly Lock _gate = new();
    private readonly Dictionary<Guid, OfferQuote> _quotes = [];
    private readonly Queue<Guid> _insertionOrder = new();

    /// <summary>Adds a quote to the store, evicting the oldest entries beyond the cap.</summary>
    /// <param name="quote">The quote to store.</param>
    public void Add(OfferQuote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);
        lock (_gate)
        {
            if (_quotes.TryAdd(quote.OfferId, quote))
            {
                _insertionOrder.Enqueue(quote.OfferId);
            }
            else
            {
                _quotes[quote.OfferId] = quote;
            }

            while (_insertionOrder.Count > MaxEntries)
            {
                _quotes.Remove(_insertionOrder.Dequeue());
            }
        }
    }

    /// <summary>Looks up a quote by id.</summary>
    /// <param name="offerId">The quote id.</param>
    /// <returns>The quote, or <see langword="null"/> when unknown or evicted.</returns>
    public OfferQuote? Get(Guid offerId)
    {
        lock (_gate)
        {
            return _quotes.GetValueOrDefault(offerId);
        }
    }
}
