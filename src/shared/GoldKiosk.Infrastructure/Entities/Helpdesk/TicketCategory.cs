namespace GoldKiosk.Infrastructure.Entities.Helpdesk;

/// <summary>
/// Maps onto <c>helpdesk.ticket_categories</c>. Global catalogue of support
/// ticket categories — NOT tenant-scoped.
/// </summary>
public sealed class TicketCategory
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;        // citext
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
}
