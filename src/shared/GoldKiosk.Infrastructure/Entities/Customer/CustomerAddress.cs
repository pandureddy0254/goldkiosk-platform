namespace GoldKiosk.Infrastructure.Entities.Customer;

/// <summary>Maps onto <c>customer.customer_addresses</c>.</summary>
public sealed class CustomerAddress
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the customer id.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the label.</summary>
    public string? Label { get; set; }
    /// <summary>Gets or sets the street enc.</summary>
    public byte[] StreetEnc { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets the city.</summary>
    public string City { get; set; } = string.Empty;
    /// <summary>Gets or sets the state or region.</summary>
    public string? StateOrRegion { get; set; }
    /// <summary>Gets or sets the postal code enc.</summary>
    public byte[]? PostalCodeEnc { get; set; }
    /// <summary>Gets or sets the country code.</summary>
    public string CountryCode { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is current.</summary>
    public bool IsCurrent { get; set; } = true;
    /// <summary>Gets or sets the valid from.</summary>
    public DateOnly? ValidFrom { get; set; }
    /// <summary>Gets or sets the valid to.</summary>
    public DateOnly? ValidTo { get; set; }
}
