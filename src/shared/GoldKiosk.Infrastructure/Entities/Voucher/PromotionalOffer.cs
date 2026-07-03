using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Voucher;

/// <summary>Maps onto <c>voucher.promotional_offers</c>.</summary>
public sealed class PromotionalOffer : ITenantScoped
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;             // citext
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the discount pct.</summary>
    public decimal? DiscountPct { get; set; }

    /// <summary>Gets or sets the validity start.</summary>
    public DateTimeOffset ValidityStart { get; set; }
    /// <summary>Gets or sets the validity end.</summary>
    public DateTimeOffset ValidityEnd { get; set; }
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the created by user id.</summary>
    public Guid? CreatedByUserId { get; set; }
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the updated by user id.</summary>
    public Guid? UpdatedByUserId { get; set; }
}
