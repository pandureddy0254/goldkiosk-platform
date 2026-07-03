namespace GoldKiosk.Infrastructure.Entities.Ops;

/// <summary>
/// Maps onto <c>ops.ticket_status_history</c>. Cross-ticket status audit trail.
/// Append-only: no <c>updated_at</c>, no soft-delete — services must never
/// <c>Update</c> or <c>Remove</c> rows of this entity.
/// </summary>
public sealed class TicketStatusHistory
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the ticket id.</summary>
    public Guid TicketId { get; set; }
    /// <summary>Gets or sets the ticket kind.</summary>
    public string TicketKind { get; set; } = string.Empty;     // deployment | maintenance | support | sos | collection
    /// <summary>Gets or sets the old status.</summary>
    public string? OldStatus { get; set; }
    /// <summary>Gets or sets the new status.</summary>
    public string NewStatus { get; set; } = string.Empty;
    /// <summary>Gets or sets the changed by user id.</summary>
    public Guid? ChangedByUserId { get; set; }
    /// <summary>Gets or sets the changed at.</summary>
    public DateTimeOffset ChangedAt { get; set; }
    /// <summary>Gets or sets the remarks.</summary>
    public string? Remarks { get; set; }
}
