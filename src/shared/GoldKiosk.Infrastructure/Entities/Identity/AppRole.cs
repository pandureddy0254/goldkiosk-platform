using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Infrastructure.Entities.Identity;

/// <summary>
/// Tenant-scoped role. NOT inherited from <c>IdentityRole</c> — we manage role/permission
/// lookups ourselves via <c>identity.role_permissions</c> rather than through Identity's
/// claims mechanism. See <c>PermissionHandler</c>.
/// </summary>
public sealed class AppRole : ITenantScoped, ISoftDeletable
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the tenant id.</summary>
    public Guid TenantId { get; set; }

    /// <summary>Short stable identifier, unique per tenant. <c>citext</c> in the DB.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary>Built-in role that tenant admins cannot edit or delete.</summary>
    public bool IsSystem { get; set; }

    /// <summary>Gets or sets a value indicating whether is active.</summary>
    public bool IsActive { get; set; } = true;
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Gets or sets the deleted at.</summary>
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation
    /// <summary>Gets or sets the role permissions.</summary>
    public ICollection<AppRolePermission> RolePermissions { get; set; } = new List<AppRolePermission>();
    /// <summary>Gets or sets the user roles.</summary>
    public ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>();
}
