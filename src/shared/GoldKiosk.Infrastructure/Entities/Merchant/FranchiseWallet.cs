namespace GoldKiosk.Infrastructure.Entities.Merchant;

/// <summary>Maps onto <c>merchant.franchise_wallets</c>.</summary>
public sealed class FranchiseWallet
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the merchant id.</summary>
    public Guid MerchantId { get; set; }

    /// <summary>Gets or sets the balance.</summary>
    public decimal Balance { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>Gets or sets the last topup at.</summary>
    public DateTimeOffset? LastTopupAt { get; set; }
    /// <summary>Gets or sets the topup threshold.</summary>
    public decimal? TopupThreshold { get; set; }

    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets the created by user id.</summary>
    public Guid? CreatedByUserId { get; set; }
    /// <summary>Gets or sets the updated by user id.</summary>
    public Guid? UpdatedByUserId { get; set; }
}
