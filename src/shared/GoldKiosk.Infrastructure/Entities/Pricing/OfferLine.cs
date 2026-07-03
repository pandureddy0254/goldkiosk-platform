namespace GoldKiosk.Infrastructure.Entities.Pricing;

/// <summary>Maps onto <c>pricing.offer_lines</c>.</summary>
public sealed class OfferLine
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the offer id.</summary>
    public Guid OfferId { get; set; }
    /// <summary>Gets or sets the transaction item id.</summary>
    public Guid TransactionItemId { get; set; }
    /// <summary>Gets or sets the metal rate id.</summary>
    public Guid MetalRateId { get; set; }
    /// <summary>Gets or sets the tier deduction id.</summary>
    public Guid? TierDeductionId { get; set; }
    /// <summary>Gets or sets the billed karat.</summary>
    public decimal BilledKarat { get; set; }
    /// <summary>Gets or sets the billed weight g.</summary>
    public decimal BilledWeightG { get; set; }
    /// <summary>Gets or sets the rate per gram.</summary>
    public decimal RatePerGram { get; set; }
    /// <summary>Gets or sets the gross value.</summary>
    public decimal GrossValue { get; set; }
    /// <summary>Gets or sets the deductions.</summary>
    public decimal Deductions { get; set; }
    /// <summary>Gets or sets the net offered.</summary>
    public decimal NetOffered { get; set; }
}
