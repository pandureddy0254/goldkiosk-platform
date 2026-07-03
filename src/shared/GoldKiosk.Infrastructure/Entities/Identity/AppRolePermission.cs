using System.Diagnostics.CodeAnalysis;

namespace GoldKiosk.Infrastructure.Entities.Identity;

/// <summary>Many-to-many link between roles and the permissions they grant.</summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Faithful port of the platform2 schema entity; the type genuinely models a role-permission link row (identity.role_permissions).")]
public sealed class AppRolePermission
{
    /// <summary>Gets or sets the role id.</summary>
    public Guid RoleId { get; set; }
    /// <summary>Gets or sets the permission id.</summary>
    public Guid PermissionId { get; set; }
    /// <summary>Gets or sets the granted at.</summary>
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets the role.</summary>
    public AppRole? Role { get; set; }
    /// <summary>Gets or sets the permission.</summary>
    public AppPermission? Permission { get; set; }
}
