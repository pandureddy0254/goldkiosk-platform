namespace GoldKiosk.Infrastructure.Entities.Merchant;

/// <summary>Maps onto <c>merchant.wallet_topup_requests</c>.</summary>
public sealed class WalletTopupRequest
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the wallet id.</summary>
    public Guid WalletId { get; set; }

    /// <summary>Gets or sets the requested amount.</summary>
    public decimal RequestedAmount { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "pending";              // pending | approved | rejected | cancelled
    /// <summary>Gets or sets the payment reference.</summary>
    public string? PaymentReference { get; set; }

    /// <summary>Gets or sets the requested at.</summary>
    public DateTimeOffset RequestedAt { get; set; }
    /// <summary>Gets or sets the approved by user id.</summary>
    public Guid? ApprovedByUserId { get; set; }
    /// <summary>Gets or sets the approved at.</summary>
    public DateTimeOffset? ApprovedAt { get; set; }
    /// <summary>Gets or sets the rejected reason.</summary>
    public string? RejectedReason { get; set; }
}
