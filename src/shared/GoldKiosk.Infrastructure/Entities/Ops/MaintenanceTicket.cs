using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Ops;

/// <summary>
/// Maps onto <c>ops.maintenance_tickets</c>. Ongoing kiosk maintenance work.
/// </summary>
public sealed class MaintenanceTicket : ITenantScoped
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;          // citext
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the device id.</summary>
    public Guid? DeviceId { get; set; }
    /// <summary>Gets or sets the ticket type.</summary>
    public string TicketType { get; set; } = "preventive";    // preventive | corrective | calibration | cleaning
    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Gets or sets the priority.</summary>
    public string Priority { get; set; } = "normal";          // low | normal | high | urgent
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "open";              // open | in_progress | resolved | closed | reopened | other
    /// <summary>Gets or sets the technician id.</summary>
    public Guid? TechnicianId { get; set; }
    /// <summary>Gets or sets the scheduled at.</summary>
    public DateTimeOffset? ScheduledAt { get; set; }
    /// <summary>Gets or sets the completed at.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
