using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Ops;

/// <summary>
/// Maps onto <c>ops.deployment_tickets</c>. Tracks initial kiosk deployment work.
/// </summary>
public sealed class DeploymentTicket : ITenantScoped
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;           // citext
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid? KioskId { get; set; }
    /// <summary>Gets or sets the store id.</summary>
    public Guid? StoreId { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "open";               // open | in_progress | resolved | closed | reopened | other
    /// <summary>Gets or sets the priority.</summary>
    public string Priority { get; set; } = "normal";           // low | normal | high | urgent
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the assigned technician id.</summary>
    public Guid? AssignedTechnicianId { get; set; }
    /// <summary>Gets or sets the opened at.</summary>
    public DateTimeOffset OpenedAt { get; set; }
    /// <summary>Gets or sets the closed at.</summary>
    public DateTimeOffset? ClosedAt { get; set; }
}
