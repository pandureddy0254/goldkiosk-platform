namespace GoldKiosk.Infrastructure.Entities.Pricing;

/// <summary>Maps onto <c>pricing.metal_rates</c> (append-only history).</summary>
public sealed class MetalRate
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the metal.</summary>
    public string Metal { get; set; } = string.Empty;
    /// <summary>Gets or sets the purity karat.</summary>
    public decimal PurityKarat { get; set; }
    /// <summary>Gets or sets the price per gram.</summary>
    public decimal PricePerGram { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the metal rate source id.</summary>
    public Guid MetalRateSourceId { get; set; }
    /// <summary>Gets or sets the retrieved at.</summary>
    public DateTimeOffset RetrievedAt { get; set; }
    /// <summary>Gets or sets the valid until.</summary>
    public DateTimeOffset? ValidUntil { get; set; }
}
