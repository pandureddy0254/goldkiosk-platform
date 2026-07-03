namespace GoldKiosk.Infrastructure.Entities.Pricing;

/// <summary>Maps onto <c>pricing.offers</c> — the per-transaction price offer
/// (NOT the promotional offer in <c>voucher.promotional_offers</c>).</summary>
public sealed class Offer
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the transaction id.</summary>
    public Guid TransactionId { get; set; }
    /// <summary>Gets or sets the pricing policy id.</summary>
    public Guid PricingPolicyId { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "draft";
    /// <summary>Gets or sets the total offered.</summary>
    public decimal TotalOffered { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the presented at.</summary>
    public DateTimeOffset? PresentedAt { get; set; }
    /// <summary>Gets or sets the expires at.</summary>
    public DateTimeOffset? ExpiresAt { get; set; }
    /// <summary>Gets or sets the accepted at.</summary>
    public DateTimeOffset? AcceptedAt { get; set; }
    /// <summary>Gets or sets the declined at.</summary>
    public DateTimeOffset? DeclinedAt { get; set; }
}
