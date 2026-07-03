namespace GoldKiosk.Cloud.AdminPortal.Models.Identity;

/// <summary>
/// Top-level view-model bound to <c>Views/RolesPermissions/Index.cshtml</c>.
/// Re-hydrated on every GET — held loosely (no entity refs) so it can also be
/// serialised cleanly into the page for client-side enable/disable toggling
/// without a round trip.
/// </summary>
public sealed class RolesIndexViewModel
{
    /// <summary>Gets or sets the kpis.</summary>
    public RolesKpiViewModel Kpis { get; init; } = new();

    /// <summary>All roles for the current tenant (system + custom, ordered system-first).</summary>
    public IReadOnlyList<RoleRow> Roles { get; init; } = Array.Empty<RoleRow>();

    /// <summary>The role whose permissions the editor pane is showing.</summary>
    public RoleRow? SelectedRole { get; init; }

    /// <summary>Permission codes granted to <see cref="SelectedRole"/>.</summary>
    public HashSet<string> GrantedPermissionCodes { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All permissions grouped by display module — the editor renders these
    /// groups in this exact order. Keys mirror the headings in the prototype:
    /// Dashboard, Kiosks, Customers, Sales · Vouchers · Reports, Support · Audit · Monitoring · Operations, Administration.
    /// </summary>
    public IReadOnlyList<PermissionModuleGroup> ModuleGroups { get; init; } = Array.Empty<PermissionModuleGroup>();
}

/// <summary>Permission module group.</summary>
public sealed class PermissionModuleGroup
{
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Gets or sets the meta.</summary>
    public string Meta { get; init; } = string.Empty;
    /// <summary>Gets or sets the permissions.</summary>
    public IReadOnlyList<PermissionRow> Permissions { get; init; } = Array.Empty<PermissionRow>();
}

/// <summary>Role row.</summary>
public sealed class RoleRow
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; init; }
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; init; } = string.Empty;
    /// <summary>Gets or sets the name.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; init; }
    /// <summary>Gets or sets a value indicating whether is system.</summary>
    public bool IsSystem { get; init; }
    /// <summary>Gets or sets the user count.</summary>
    public int UserCount { get; init; }
    /// <summary>Gets or sets the permission count.</summary>
    public int PermissionCount { get; init; }
    /// <summary>Gets or sets the created at.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Suggested pill css class for this role.
    /// Mirrors the .role-pill r-{admin|manager|operator|analyst|support|auditor}
    /// classes already defined in main.css. Custom roles return <c>r-custom</c>
    /// which the view paints with the --info token inline.
    /// </summary>
    public string PillClass => Code.ToLowerInvariant() switch
    {
        "owner" => "r-admin",
        "manager" => "r-manager",
        "operator" => "r-operator",
        "analyst" => "r-analyst",
        "support" => "r-support",
        "auditor" => "r-auditor",
        _ => "r-custom",
    };

    /// <summary>Short label shown inside the pill.</summary>
    public string PillLabel => Code.ToLowerInvariant() switch
    {
        "owner" => "Primary admin",
        "manager" => "Manager",
        "operator" => "Operator",
        "analyst" => "Analyst",
        "support" => "Support",
        "auditor" => "Auditor",
        _ => "Custom",
    };
}

/// <summary>Permission row.</summary>
public sealed class PermissionRow
{
    /// <summary>Gets or sets the code.</summary>
    public string Code { get; init; } = string.Empty;
    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; init; } = string.Empty;
    /// <summary>Gets or sets the description.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Sensitive perms render with the .perm-tag.warn "sensitive" badge.
    /// Includes tenant:admin, users:admin, customers:unmask, api_credentials:admin
    /// — the four that the prototype tags. Server-side enforcement is unchanged;
    /// this is a display affordance only.
    /// </summary>
    public bool IsSensitive { get; init; }

    /// <summary>
    /// PII unmask / audit-tracked perms render with the neutral "audited" tag
    /// instead of "sensitive". Currently only <c>customers:unmask</c>.
    /// </summary>
    public bool IsAudited { get; init; }
}

/// <summary>Roles kpi view model.</summary>
public sealed class RolesKpiViewModel
{
    /// <summary>Gets or sets the total roles.</summary>
    public int TotalRoles { get; init; }
    /// <summary>Gets or sets the system roles.</summary>
    public int SystemRoles { get; init; }
    /// <summary>Gets or sets the custom roles.</summary>
    public int CustomRoles { get; init; }
    /// <summary>Gets or sets the total permissions.</summary>
    public int TotalPermissions { get; init; }
    /// <summary>Gets or sets the users assigned.</summary>
    public int UsersAssigned { get; init; }

    /// <summary>Permission-change events written to audit.audit_events over the past 7 days.</summary>
    public int ChangesLast7d { get; init; }
}
