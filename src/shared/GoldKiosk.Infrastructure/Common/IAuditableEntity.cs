namespace GoldKiosk.Infrastructure.Common;

/// <summary>
/// Implemented by entities that carry the standard audit columns.
/// The <c>AuditActorInterceptor</c> uses this to maintain <see cref="CreatedAt"/>,
/// <see cref="UpdatedAt"/>, <see cref="CreatedByUserId"/>, and <see cref="UpdatedByUserId"/>
/// automatically on <c>SaveChanges</c>.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>Gets or sets when the row was created (UTC).</summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the row was last updated (UTC).</summary>
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Gets or sets the id of the user who created the row, or null for system writes.</summary>
    Guid? CreatedByUserId { get; set; }

    /// <summary>Gets or sets the id of the user who last updated the row, or null for system writes.</summary>
    Guid? UpdatedByUserId { get; set; }
}

/// <summary>
/// Implemented by entities that participate in soft-delete semantics
/// (i.e. they have a <c>deleted_at</c> column).
/// </summary>
public interface ISoftDeletable
{
    /// <summary>Gets or sets when the row was soft-deleted (UTC), or null while it is live.</summary>
    DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>
/// Implemented by entities owned by a tenant. <c>TenantContextInterceptor</c>
/// sets the PG <c>app.tenant_id</c> GUC before every query so RLS policies kick in;
/// services additionally filter by <c>TenantId</c> as defence in depth.
/// </summary>
public interface ITenantScoped
{
    /// <summary>Gets or sets the id of the tenant that owns the row.</summary>
    Guid TenantId { get; set; }
}
