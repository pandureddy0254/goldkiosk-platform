using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Ops;

/// <summary>
/// Maps onto <c>ops.technician_clusters</c>. A regional grouping of technicians.
/// No audit columns on the underlying table — does not implement
/// <see cref="IAuditableEntity"/>.
/// </summary>
public sealed class TechnicianCluster : ITenantScoped
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; set; } = string.Empty;       // citext
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
}
