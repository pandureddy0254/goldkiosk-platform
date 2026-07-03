namespace GoldKiosk.Infrastructure.Entities.Customer;

/// <summary>Maps onto <c>customer.customer_contacts</c>.</summary>
public sealed class CustomerContact
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the customer id.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the channel.</summary>
    public string Channel { get; set; } = string.Empty;   // 'email' | 'phone' | 'sms'
    /// <summary>Gets or sets the value enc.</summary>
    public byte[] ValueEnc { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets the value lookup hash.</summary>
    public byte[] ValueLookupHash { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets a value indicating whether is verified.</summary>
    public bool IsVerified { get; set; }
    /// <summary>Gets or sets the verified at.</summary>
    public DateTimeOffset? VerifiedAt { get; set; }
    /// <summary>Gets or sets a value indicating whether is primary.</summary>
    public bool IsPrimary { get; set; }
}
