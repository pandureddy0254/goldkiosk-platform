using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Customer;

/// <summary>Maps onto <c>customer.customer_identifiers</c>.</summary>
public sealed class CustomerIdentifier : ISoftDeletable
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the customer id.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the id type.</summary>
    public string IdType { get; set; } = string.Empty;
    /// <summary>Gets or sets the id number enc.</summary>
    public byte[] IdNumberEnc { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets the id number lookup hash.</summary>
    public byte[] IdNumberLookupHash { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets the issuing country.</summary>
    public string IssuingCountry { get; set; } = string.Empty;
    /// <summary>Gets or sets the issued on.</summary>
    public DateOnly? IssuedOn { get; set; }
    /// <summary>Gets or sets the expires on.</summary>
    public DateOnly? ExpiresOn { get; set; }
    /// <summary>Gets or sets a value indicating whether is primary.</summary>
    public bool IsPrimary { get; set; }
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Gets or sets the deleted at.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
