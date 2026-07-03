using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Ops;

/// <summary>
/// Maps onto <c>ops.technicians</c>. A field-services technician assigned to a
/// <see cref="TechnicianCluster"/>. PII columns (<c>email_enc</c>, <c>mobile_enc</c>)
/// are application-encrypted bytea and not exposed by list services.
/// </summary>
public sealed class Technician : ITenantScoped
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }
    /// <summary>Gets or sets the user id.</summary>
    public Guid? UserId { get; set; }
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the email enc.</summary>
    public byte[]? EmailEnc { get; set; }
    /// <summary>Gets or sets the mobile enc.</summary>
    public byte[]? MobileEnc { get; set; }
    /// <summary>Gets or sets the cluster id.</summary>
    public Guid ClusterId { get; set; }
    /// <summary>Gets or sets the status.</summary>
    public string Status { get; set; } = "active";        // active | on_leave | inactive
    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
