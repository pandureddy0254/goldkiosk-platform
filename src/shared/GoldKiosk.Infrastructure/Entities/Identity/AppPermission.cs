using System.Diagnostics.CodeAnalysis;

namespace GoldKiosk.Infrastructure.Entities.Identity;

/// <summary>
/// Catalog of every permission a role can grant. Global (not tenant-scoped) so the same
/// code means the same thing across the platform.
/// </summary>
[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Faithful port of the platform2 schema entity; the type genuinely models a permission row (identity.permissions).")]
public sealed class AppPermission
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }

    /// <summary>Globally unique permission code, e.g. <c>transaction.refund</c>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; set; }

    /// <summary><c>tenant</c> | <c>store</c> | <c>kiosk</c> — level at which a grant can be scoped.</summary>
    public string Scope { get; set; } = "tenant";

    // Navigation
    /// <summary>Gets or sets the role permissions.</summary>
    public ICollection<AppRolePermission> RolePermissions { get; set; } = new List<AppRolePermission>();
}
