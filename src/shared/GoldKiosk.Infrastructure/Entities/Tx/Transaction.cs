using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Tx;

/// <summary>Maps onto <c>tx.transactions</c>.</summary>
public sealed class Transaction : ITenantScoped
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the store id.</summary>
    public Guid StoreId { get; set; }
    /// <summary>Gets or sets the customer id.</summary>
    public Guid CustomerId { get; set; }
    /// <summary>Gets or sets the transaction code.</summary>
    public string TransactionCode { get; set; } = string.Empty;     // citext
    /// <summary>Gets or sets the kind.</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "started";
    /// <summary>Gets or sets the total amount.</summary>
    public decimal TotalAmount { get; set; }
    /// <summary>Gets or sets the fees.</summary>
    public decimal Fees { get; set; }
    /// <summary>Gets or sets the net payout.</summary>
    public decimal NetPayout { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the source.</summary>
    public string? Source { get; set; }
    /// <summary>Gets or sets the coordinator user id.</summary>
    public Guid? CoordinatorUserId { get; set; }
    /// <summary>Gets or sets the rejection reason id.</summary>
    public Guid? RejectionReasonId { get; set; }
    /// <summary>Gets or sets a value indicating whether is junk.</summary>
    public bool IsJunk { get; set; }
    /// <summary>Gets or sets the started at.</summary>
    public DateTimeOffset StartedAt { get; set; }
    /// <summary>Gets or sets the completed at.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
    /// <summary>Gets or sets the occurred at.</summary>
    public DateTimeOffset OccurredAt { get; set; }
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Gets or sets the updated at.</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Added in db/0030 for the AdminDashboard reversal flow.
    /// <summary>Gets or sets the reversed at.</summary>
    public DateTimeOffset? ReversedAt { get; set; }
    /// <summary>Gets or sets the reversed by user id.</summary>
    public Guid? ReversedByUserId { get; set; }
}
