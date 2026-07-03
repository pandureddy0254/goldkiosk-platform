using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Helpdesk;

/// <summary>
/// Maps onto <c>helpdesk.support_tickets</c>. Tenant-scoped support ticket.
/// Append-only timeline (no <c>updated_at</c> column) — does NOT implement
/// <see cref="IAuditableEntity"/>.
/// </summary>
public sealed class SupportTicket : ITenantScoped
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;            // citext
    /// <summary>Gets or sets the category id.</summary>
    public Guid CategoryId { get; set; }
    /// <summary>Gets or sets the sub category id.</summary>
    public Guid? SubCategoryId { get; set; }
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "open";                // open | in_progress | waiting_customer | resolved | closed | reopened
    /// <summary>Gets or sets the remarks.</summary>
    public string? Remarks { get; set; }
    /// <summary>Gets or sets the documents URI.</summary>
    public string? DocumentsUri { get; set; }
    /// <summary>Gets or sets the created by user id.</summary>
    public Guid CreatedByUserId { get; set; }
    /// <summary>Gets or sets the closed by user id.</summary>
    public Guid? ClosedByUserId { get; set; }
    /// <summary>Gets or sets the reopened by user id.</summary>
    public Guid? ReopenedByUserId { get; set; }
    /// <summary>Gets or sets a value indicating whether is closed.</summary>
    public bool IsClosed { get; set; }
    /// <summary>Gets or sets a value indicating whether is reopened.</summary>
    public bool IsReopened { get; set; }
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>Gets or sets the closed at.</summary>
    public DateTimeOffset? ClosedAt { get; set; }
    /// <summary>Gets or sets the reopened at.</summary>
    public DateTimeOffset? ReopenedAt { get; set; }
}
