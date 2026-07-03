using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Customer;

/// <summary>
/// Maps onto <c>customer.customers</c>. Customer-master row. PII columns (<c>*_enc</c>)
/// are encrypted at the application layer and must never be projected into list views.
/// </summary>
public sealed class Customer : ITenantScoped, ISoftDeletable
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the customer code.</summary>
    public string CustomerCode { get; set; } = string.Empty;        // citext
    /// <summary>Gets or sets the given name enc.</summary>
    public byte[] GivenNameEnc { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets the family name enc.</summary>
    public byte[] FamilyNameEnc { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets the date of birth enc.</summary>
    public byte[]? DateOfBirthEnc { get; set; }
    /// <summary>Gets or sets the gender.</summary>
    public string? Gender { get; set; }
    /// <summary>Gets or sets the primary nationality.</summary>
    public string? PrimaryNationality { get; set; }
    /// <summary>Gets or sets the mobile lookup hash.</summary>
    public byte[]? MobileLookupHash { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "prospect";

    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Gets or sets the updated at.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
    /// <summary>Gets or sets the deleted at.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
