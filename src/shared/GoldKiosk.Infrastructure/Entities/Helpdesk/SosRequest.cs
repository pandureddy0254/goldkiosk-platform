using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Helpdesk;

/// <summary>
/// Maps onto <c>helpdesk.sos_requests</c>. A panic/SOS request raised from a
/// kiosk or by a customer that flows through acknowledgement, dispatch, and
/// resolution. Tenant-scoped.
/// </summary>
public sealed class SosRequest : ITenantScoped
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid? KioskId { get; set; }
    /// <summary>Gets or sets the customer id.</summary>
    public Guid? CustomerId { get; set; }
    /// <summary>Gets or sets the priority.</summary>
    public string Priority { get; set; } = "high";              // low | medium | high | critical
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "raised";              // raised | acknowledged | dispatched | resolved | cancelled
    /// <summary>Gets or sets the assigned technician id.</summary>
    public Guid? AssignedTechnicianId { get; set; }
    /// <summary>Gets or sets the raised at.</summary>
    public DateTimeOffset RaisedAt { get; set; }
    /// <summary>Gets or sets the acknowledged at.</summary>
    public DateTimeOffset? AcknowledgedAt { get; set; }
    /// <summary>Gets or sets the resolved at.</summary>
    public DateTimeOffset? ResolvedAt { get; set; }
}
