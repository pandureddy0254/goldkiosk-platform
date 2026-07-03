using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Helpdesk;

/// <summary>
/// Maps onto <c>helpdesk.feedback</c>. Customer-submitted feedback recorded
/// at a kiosk. Append-only (no <c>updated_at</c> column) — does NOT implement
/// <see cref="IAuditableEntity"/>.
/// </summary>
public sealed class Feedback : ITenantScoped
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid? KioskId { get; set; }
    /// <summary>Gets or sets the customer id.</summary>
    public Guid? CustomerId { get; set; }
    /// <summary>Gets or sets the function name.</summary>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the feedback type.</summary>
    public string FeedbackType { get; set; } = string.Empty;     // compliment | complaint | suggestion | question
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the audio URL.</summary>
    public string? AudioUrl { get; set; }
    /// <summary>Gets or sets the image URL.</summary>
    public string? ImageUrl { get; set; }
    /// <summary>Gets or sets the location text.</summary>
    public string? LocationText { get; set; }
    /// <summary>Gets or sets a value indicating whether is read.</summary>
    public bool IsRead { get; set; }
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
