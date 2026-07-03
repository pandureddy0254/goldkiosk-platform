namespace GoldKiosk.Infrastructure.Entities.Helpdesk;

/// <summary>
/// Maps onto <c>helpdesk.ticket_sub_categories</c>. Child of <see cref="TicketCategory"/>.
/// Global catalogue — NOT tenant-scoped.
/// </summary>
public sealed class TicketSubCategory
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the parent category id.</summary>
    public Guid ParentCategoryId { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;        // citext
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
}
