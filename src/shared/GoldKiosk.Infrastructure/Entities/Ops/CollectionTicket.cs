namespace GoldKiosk.Infrastructure.Entities.Ops;

/// <summary>
/// Maps onto <c>ops.collection_tickets</c>. One ticket per kiosk visited within
/// a <see cref="CollectionRun"/>. Tenant scope is reached through the parent run.
/// </summary>
public sealed class CollectionTicket
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the collection run id.</summary>
    public Guid CollectionRunId { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the amount collected.</summary>
    public decimal AmountCollected { get; set; }
    /// <summary>Gets or sets the currency code.</summary>
    public string CurrencyCode { get; set; } = string.Empty;
    /// <summary>Gets or sets the items collected.</summary>
    public int ItemsCollected { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "pending";          // pending | collected | skipped | discrepancy
    /// <summary>Gets or sets the collected at.</summary>
    public DateTimeOffset? CollectedAt { get; set; }
    /// <summary>Gets or sets the signed off by user id.</summary>
    public Guid? SignedOffByUserId { get; set; }
}
