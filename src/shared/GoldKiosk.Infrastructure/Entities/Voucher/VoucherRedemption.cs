namespace GoldKiosk.Infrastructure.Entities.Voucher;

/// <summary>Maps onto <c>voucher.voucher_redemptions</c>. Read-only from the dashboard.</summary>
public sealed class VoucherRedemption
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the voucher id.</summary>
    public Guid VoucherId { get; set; }
    /// <summary>Gets or sets the customer id.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the transaction id.</summary>
    public Guid TransactionId { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }

    /// <summary>Gets or sets the redeemed amount.</summary>
    public decimal RedeemedAmount { get; set; }
    /// <summary>Gets or sets the remaining balance.</summary>
    public decimal? RemainingBalance { get; set; }

    /// <summary>Gets or sets the redeemed at.</summary>
    public DateTimeOffset RedeemedAt { get; set; }
}
