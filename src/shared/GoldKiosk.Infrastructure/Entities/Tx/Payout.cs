namespace GoldKiosk.Infrastructure.Entities.Tx;

/// <summary>Maps onto <c>payment.payouts</c>. Lives in the Tx folder because it's
/// surfaced from the transaction-history view; the actual table is in the
/// <c>payment</c> schema.</summary>
public sealed class Payout
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the payment id.</summary>
    public Guid PaymentId { get; set; }
    /// <summary>Gets or sets the payout provider id.</summary>
    public Guid PayoutProviderId { get; set; }
    /// <summary>Gets or sets the external reference.</summary>
    public string? ExternalReference { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "queued";
    /// <summary>Gets or sets the fee amount.</summary>
    public decimal FeeAmount { get; set; }
    /// <summary>Gets or sets the sent at.</summary>
    public DateTimeOffset? SentAt { get; set; }
    /// <summary>Gets or sets the settled at.</summary>
    public DateTimeOffset? SettledAt { get; set; }
}
