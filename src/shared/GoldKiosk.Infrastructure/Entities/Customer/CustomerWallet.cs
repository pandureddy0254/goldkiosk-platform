namespace GoldKiosk.Infrastructure.Entities.Customer;

/// <summary>Maps onto <c>customer.customer_wallets</c>. Balance is maintained by the
/// AFTER-INSERT trigger on <c>wallet_ledger_entries</c>; never write it directly.</summary>
public sealed class CustomerWallet
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the customer id.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the balance.</summary>
    public decimal Balance { get; set; }
    /// <summary>Gets or sets the PIN hash.</summary>
    public string PinHash { get; set; } = string.Empty;
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "active";
    /// <summary>Gets or sets the last updated at.</summary>
    public DateTimeOffset LastUpdatedAt { get; set; }
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
