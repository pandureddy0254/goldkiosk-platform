namespace GoldKiosk.Infrastructure.Entities.Customer;

/// <summary>Maps onto <c>customer.customer_profiles</c>. 1:1 with <see cref="Customer"/>.</summary>
public sealed class CustomerProfile
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the customer id.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the occupation.</summary>
    public string? Occupation { get; set; }
    /// <summary>Gets or sets the source of funds.</summary>
    public string? SourceOfFunds { get; set; }
    /// <summary>Gets or sets the preferred language.</summary>
    public string? PreferredLanguage { get; set; }
    /// <summary>Gets or sets a value indicating whether marketing opt in.</summary>
    public bool MarketingOptIn { get; set; }
    /// <summary>Gets or sets the risk band.</summary>
    public string? RiskBand { get; set; }
}
