namespace GoldKiosk.Infrastructure.Entities.Voucher;

/// <summary>Maps onto <c>voucher.redemption_policies</c>.</summary>
public sealed class RedemptionPolicy
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the voucher id.</summary>
    public Guid VoucherId { get; set; }

    /// <summary>Gets or sets the min purchase amount.</summary>
    public decimal? MinPurchaseAmount { get; set; }
    /// <summary>Gets or sets the max discount cap.</summary>
    public decimal? MaxDiscountCap { get; set; }
    /// <summary>Gets or sets the per customer limit.</summary>
    public int? PerCustomerLimit { get; set; }
    /// <summary>Gets or sets the applicable categories.</summary>
    public string? ApplicableCategories { get; set; }            // jsonb stored as text

    /// <summary>Gets or sets the effective from.</summary>
    public DateTimeOffset EffectiveFrom { get; set; }
    /// <summary>Gets or sets the effective to.</summary>
    public DateTimeOffset? EffectiveTo { get; set; }
}
