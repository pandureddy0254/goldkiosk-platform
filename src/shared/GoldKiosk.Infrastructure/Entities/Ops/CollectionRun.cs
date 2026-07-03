using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Ops;

/// <summary>
/// Maps onto <c>ops.collection_runs</c>. A scheduled visit-route that collects
/// metal/cash from a set of kiosks.
/// </summary>
public sealed class CollectionRun : ITenantScoped
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;           // citext
    /// <summary>Gets or sets the run date.</summary>
    public DateOnly RunDate { get; set; }
    /// <summary>Gets or sets the lead technician id.</summary>
    public Guid? LeadTechnicianId { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "planned";            // planned | in_progress | completed | cancelled
    /// <summary>Gets or sets the total amount.</summary>
    public decimal TotalAmount { get; set; }
    /// <summary>Gets or sets the total items.</summary>
    public int TotalItems { get; set; }
    /// <summary>Gets or sets the started at.</summary>
    public DateTimeOffset? StartedAt { get; set; }
    /// <summary>Gets or sets the completed at.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
