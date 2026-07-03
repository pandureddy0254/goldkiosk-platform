using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Merchant;

/// <summary>Maps onto <c>merchant.merchants</c>. A B2B franchise / partner merchant.</summary>
public sealed class Merchant : ITenantScoped, ISoftDeletable
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;             // citext
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the email enc.</summary>
    public byte[]? EmailEnc { get; set; }
    /// <summary>Gets or sets the mobile enc.</summary>
    public byte[]? MobileEnc { get; set; }
    /// <summary>Gets or sets the address enc.</summary>
    public byte[]? AddressEnc { get; set; }

    /// <summary>Gets or sets the KYC status.</summary>
    public string KycStatus { get; set; } = "pending";            // pending | approved | rejected | review
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Gets or sets the deleted at.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Gets or sets the created by user id.</summary>
    public Guid? CreatedByUserId { get; set; }
    /// <summary>Gets or sets the updated by user id.</summary>
    public Guid? UpdatedByUserId { get; set; }
}
