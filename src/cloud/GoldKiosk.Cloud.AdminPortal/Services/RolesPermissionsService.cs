using GoldKiosk.Cloud.AdminPortal.Logging;
using System.Text.Json;
using GoldKiosk.Cloud.AdminPortal.Models.Identity;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Audit;
using GoldKiosk.Infrastructure.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Backing service for the Roles &amp; permissions editor (one-stop interface for the
/// 6 system roles + tenant-defined custom roles + their permission grants).
/// </summary>
/// <remarks>
/// All writes refuse to mutate system roles (<c>is_system = true</c>) — the UI
/// disables the toggles, but the server enforces it too. Each write also stamps
/// an <c>audit.audit_events</c> row synchronously before returning.
/// </remarks>
public sealed class RolesPermissionsService(
    AppDbContext db,
    ICurrentUserService currentUser,
    ILogger<RolesPermissionsService> logger) : IRolesPermissionsService
{
    // System role codes — must never be createable as a "custom" role nor deletable.
    private static readonly HashSet<string> SystemRoleCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "owner", "manager", "operator", "analyst", "support", "auditor",
    };

    // The four permissions that render with the "sensitive" warn badge.
    private static readonly HashSet<string> SensitivePerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "users:admin", "tenant:admin", "api_credentials:admin", "roles:write",
    };

    // Perms whose use is audited and rendered with the neutral "audited" tag.
    private static readonly HashSet<string> AuditedPerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "customers:unmask",
    };

    // Mapping of every permission code → display module group + sort order.
    // Order here determines the order inside each group; group order is set
    // in the static module list further down.
    private static readonly (string Module, string Code)[] PermissionModuleMap =
    [
        ("Dashboard",                                       "dashboard:read"),

        ("Kiosks",                                          "kiosks:read"),
        ("Kiosks",                                          "kiosks:write"),
        ("Kiosks",                                          "kiosks:admin"),

        ("Customers",                                       "customers:read"),
        ("Customers",                                       "customers:write"),
        ("Customers",                                       "customers:unmask"),
        ("Customers",                                       "merchants:read"),
        ("Customers",                                       "merchants:write"),

        ("Sales · Vouchers · Reports",                      "sales:read"),
        ("Sales · Vouchers · Reports",                      "sales:write"),
        ("Sales · Vouchers · Reports",                      "vouchers:read"),
        ("Sales · Vouchers · Reports",                      "vouchers:write"),
        ("Sales · Vouchers · Reports",                      "reports:read"),
        ("Sales · Vouchers · Reports",                      "reports:export"),

        ("Support · Audit · Monitoring · Operations",       "helpdesk:read"),
        ("Support · Audit · Monitoring · Operations",       "helpdesk:write"),
        ("Support · Audit · Monitoring · Operations",       "sos:read"),
        ("Support · Audit · Monitoring · Operations",       "sos:write"),
        ("Support · Audit · Monitoring · Operations",       "feedback:read"),
        ("Support · Audit · Monitoring · Operations",       "feedback:write"),
        ("Support · Audit · Monitoring · Operations",       "audit:read"),
        ("Support · Audit · Monitoring · Operations",       "audit:export"),
        ("Support · Audit · Monitoring · Operations",       "monitoring:read"),
        ("Support · Audit · Monitoring · Operations",       "operations:read"),
        ("Support · Audit · Monitoring · Operations",       "operations:write"),

        ("Administration",                                  "users:read"),
        ("Administration",                                  "users:write"),
        ("Administration",                                  "users:admin"),
        ("Administration",                                  "roles:read"),
        ("Administration",                                  "roles:write"),
        ("Administration",                                  "tenant:read"),
        ("Administration",                                  "tenant:write"),
        ("Administration",                                  "tenant:admin"),
        ("Administration",                                  "api_credentials:read"),
        ("Administration",                                  "api_credentials:write"),
        ("Administration",                                  "api_credentials:admin"),
        ("Administration",                                  "webhooks:read"),
        ("Administration",                                  "webhooks:write"),
    ];

    private static readonly (string Module, string Meta)[] ModuleOrder =
    [
        ("Dashboard",                                       "Operator home, KPI overview"),
        ("Kiosks",                                          "Fleet config, deployment, decommission"),
        ("Customers",                                       "Customer records, merchants, KYC, PII unmask"),
        ("Sales · Vouchers · Reports",                      "Commerce primitives"),
        ("Support · Audit · Monitoring · Operations",       "Day-2 ops surfaces"),
        ("Administration",                                  "Users, roles, tenant, API credentials, webhooks"),
    ];

    /// <summary>Get index.</summary>
    public async Task<RolesIndexViewModel> GetIndexAsync(Guid? selectedRoleId, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return EmptyModel();
        }

        // ── 1. permissions catalogue (global, 38 rows) ────────────────────────
        var allPerms = await db.Permissions.AsNoTracking()
            .OrderBy(p => p.Code)
            .Select(p => new { p.Code, p.DisplayName, p.Description })
            .ToListAsync(ct);

        var permsByCode = allPerms.ToDictionary(p => p.Code, p => p, StringComparer.OrdinalIgnoreCase);

        // Group + order according to the static map above.
        var moduleGroups = ModuleOrder.Select(m =>
        {
            var rows = PermissionModuleMap
                .Where(x => x.Module == m.Module)
                .Select(x => permsByCode.TryGetValue(x.Code, out var p)
                    ? new PermissionRow
                    {
                        Code = p.Code,
                        DisplayName = p.DisplayName,
                        Description = p.Description,
                        IsSensitive = SensitivePerms.Contains(p.Code),
                        IsAudited = AuditedPerms.Contains(p.Code),
                    }
                    : null)
                .Where(p => p is not null)
                .Cast<PermissionRow>()
                .ToList();

            return new PermissionModuleGroup
            {
                Name = m.Module,
                Meta = m.Meta,
                Permissions = rows,
            };
        }).ToList();

        // ── 2. tenant's roles with user + permission counts ───────────────────
        // Two queries — single-row group counts in SQL are fragile across providers
        // and the role list is tiny (≤ 30 rows) so we read it directly and
        // accumulate the counts in memory.
        var roleRowsRaw = await db.Roles.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.DeletedAt == null)
            .OrderByDescending(r => r.IsSystem)
            .ThenBy(r => r.Code)
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Name,
                r.Description,
                r.IsSystem,
                r.CreatedAt,
            })
            .ToListAsync(ct);

        var roleIds = roleRowsRaw.Select(r => r.Id).ToList();

        var permCountByRole = await db.RolePermissions.AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId))
            .GroupBy(rp => rp.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.RoleId, g => g.Count, ct);

        var userCountByRole = await db.UserRoles.AsNoTracking()
            .Where(ur => roleIds.Contains(ur.RoleId) && ur.RevokedAt == null)
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Select(x => x.UserId).Distinct().Count() })
            .ToDictionaryAsync(g => g.RoleId, g => g.Count, ct);

        var roles = roleRowsRaw.Select(r => new RoleRow
        {
            Id = r.Id,
            Code = r.Code,
            Name = r.Name,
            Description = r.Description,
            IsSystem = r.IsSystem,
            UserCount = userCountByRole.GetValueOrDefault(r.Id, 0),
            PermissionCount = permCountByRole.GetValueOrDefault(r.Id, 0),
            CreatedAt = r.CreatedAt,
        }).ToList();

        // ── 3. selected role + its grants ─────────────────────────────────────
        var selected = (selectedRoleId is Guid sid ? roles.FirstOrDefault(r => r.Id == sid) : null)
            ?? roles.FirstOrDefault(r => string.Equals(r.Code, "owner", StringComparison.OrdinalIgnoreCase))
            ?? roles.FirstOrDefault();

        HashSet<string> granted = new(StringComparer.OrdinalIgnoreCase);
        if (selected is not null)
        {
            granted = (await db.RolePermissions.AsNoTracking()
                .Where(rp => rp.RoleId == selected.Id)
                .Join(db.Permissions.AsNoTracking(),
                    rp => rp.PermissionId,
                    p => p.Id,
                    (rp, p) => p.Code)
                .ToListAsync(ct))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        // ── 4. KPIs ───────────────────────────────────────────────────────────
        var sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-7);
        var changesLast7d = await db.AuditEvents.AsNoTracking()
            .Where(a => a.TenantId == tenantId
                     && a.OccurredAt >= sevenDaysAgo
                     && a.Module == "roles"
                     && (a.Activity == "role.permissions.updated"
                      || a.Activity == "role.created"
                      || a.Activity == "role.deleted"))
            .CountAsync(ct);

        var usersAssigned = await db.UserRoles.AsNoTracking()
            .Where(ur => roleIds.Contains(ur.RoleId) && ur.RevokedAt == null)
            .Select(ur => ur.UserId)
            .Distinct()
            .CountAsync(ct);

        var kpis = new RolesKpiViewModel
        {
            TotalRoles = roles.Count,
            SystemRoles = roles.Count(r => r.IsSystem),
            CustomRoles = roles.Count(r => !r.IsSystem),
            TotalPermissions = allPerms.Count,
            UsersAssigned = usersAssigned,
            ChangesLast7d = changesLast7d,
        };

        return new RolesIndexViewModel
        {
            Kpis = kpis,
            Roles = roles,
            SelectedRole = selected,
            GrantedPermissionCodes = granted,
            ModuleGroups = moduleGroups,
        };
    }

    /// <summary>Create custom role.</summary>
    public async Task<OperationResult<Guid>> CreateCustomRoleAsync(
        string code,
        string name,
        string? description,
        IList<string> permissionCodes,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult<Guid>.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            return OperationResult<Guid>.Fail("Role code and name are required.");
        }

        code = code.Trim();
        name = name.Trim();

        if (SystemRoleCodes.Contains(code))
        {
            return OperationResult<Guid>.Fail($"Role code '{code}' is reserved for a system role.");
        }

        // Block prefix collisions too — e.g. someone trying to name a custom role
        // "owner-2" can be confusing. We accept it but block exact matches above;
        // the unique index on (tenant_id, code) handles real duplicates.
        var duplicate = await db.Roles
            .AnyAsync(r => r.TenantId == tenantId && r.Code == code && r.DeletedAt == null, ct);
        if (duplicate)
        {
            return OperationResult<Guid>.Fail($"A role with code '{code}' already exists.");
        }

        var roleId = Guid.NewGuid();
        var role = new AppRole
        {
            Id = roleId,
            TenantId = tenantId,
            Code = code,
            Name = name,
            Description = description,
            IsSystem = false,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Roles.Add(role);

        // Look up the requested permission ids in one round-trip.
        var requested = (permissionCodes ?? Array.Empty<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requested.Count > 0)
        {
            var permIds = await db.Permissions.AsNoTracking()
                .Where(p => requested.Contains(p.Code))
                .Select(p => p.Id)
                .ToListAsync(ct);

            foreach (var pid in permIds)
            {
                db.RolePermissions.Add(new AppRolePermission
                {
                    RoleId = roleId,
                    PermissionId = pid,
                    GrantedAt = DateTimeOffset.UtcNow,
                });
            }
        }

        db.AuditEvents.Add(NewAuditEvent(
            tenantId, actorUserId,
            activity: "role.created",
            targetId: roleId,
            afterJson: SerializeAudit(new { code, name, description, permissions = requested })));

        await db.SaveChangesAsync(ct);
        logger.CustomRoleCreated(code, requested.Count, actorUserId);

        return OperationResult<Guid>.Ok(roleId);
    }

    /// <summary>Update permissions.</summary>
    public async Task<OperationResult> UpdatePermissionsAsync(
        Guid roleId,
        IList<string> permissionCodes,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var role = await db.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == tenantId && r.DeletedAt == null, ct);

        if (role is null)
        {
            return OperationResult.Fail("Role not found.");
        }

        if (role.IsSystem)
        {
            return OperationResult.Fail("System roles cannot be modified. Duplicate the role as a custom role first.");
        }

        // Resolve requested codes → permission ids.
        var requested = (permissionCodes ?? Array.Empty<string>())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var requestedIds = await db.Permissions.AsNoTracking()
            .Where(p => requested.Contains(p.Code))
            .Select(p => new { p.Id, p.Code })
            .ToListAsync(ct);

        var desired = requestedIds.Select(x => x.Id).ToHashSet();

        var existing = await db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(ct);

        var existingIds = existing.Select(x => x.PermissionId).ToHashSet();

        var toAdd = desired.Except(existingIds).ToList();
        var toRemove = existing.Where(rp => !desired.Contains(rp.PermissionId)).ToList();

        foreach (var pid in toAdd)
        {
            db.RolePermissions.Add(new AppRolePermission
            {
                RoleId = roleId,
                PermissionId = pid,
                GrantedAt = DateTimeOffset.UtcNow,
            });
        }
        db.RolePermissions.RemoveRange(toRemove);

        if (toAdd.Count > 0 || toRemove.Count > 0)
        {
            db.AuditEvents.Add(NewAuditEvent(
                tenantId, actorUserId,
                activity: "role.permissions.updated",
                targetId: roleId,
                beforeJson: SerializeAudit(new { permissions = existing.Select(x => x.PermissionId).ToList() }),
                afterJson: SerializeAudit(new
                {
                    permissions = requested,
                    added = toAdd.Count,
                    removed = toRemove.Count,
                })));
        }

        await db.SaveChangesAsync(ct);
        logger.RolePermissionsUpdated(roleId, toAdd.Count, toRemove.Count, actorUserId);

        return OperationResult.Ok();
    }

    /// <summary>Delete custom role.</summary>
    public async Task<OperationResult> DeleteCustomRoleAsync(
        Guid roleId,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var role = await db.Roles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == tenantId && r.DeletedAt == null, ct);

        if (role is null)
        {
            return OperationResult.Fail("Role not found.");
        }

        if (role.IsSystem)
        {
            return OperationResult.Fail("System roles cannot be deleted.");
        }

        // Block deletion if any users still hold the role — they'd be orphaned.
        var inUse = await db.UserRoles.AnyAsync(
            ur => ur.RoleId == roleId && ur.RevokedAt == null, ct);
        if (inUse)
        {
            return OperationResult.Fail("Role is still assigned to users. Reassign them before deleting.");
        }

        role.DeletedAt = DateTimeOffset.UtcNow;
        role.IsActive = false;

        db.AuditEvents.Add(NewAuditEvent(
            tenantId, actorUserId,
            activity: "role.deleted",
            targetId: roleId,
            beforeJson: SerializeAudit(new { code = role.Code, name = role.Name })));

        await db.SaveChangesAsync(ct);
        logger.CustomRoleDeleted(roleId, role.Code, actorUserId);

        return OperationResult.Ok();
    }

    // ─── helpers ───────────────────────────────────────────────────────────────

    private static RolesIndexViewModel EmptyModel() => new()
    {
        Kpis = new RolesKpiViewModel(),
        Roles = Array.Empty<RoleRow>(),
        SelectedRole = null,
        GrantedPermissionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase),
        ModuleGroups = Array.Empty<PermissionModuleGroup>(),
    };

    private static AuditEvent NewAuditEvent(
        Guid tenantId,
        Guid actorUserId,
        string activity,
        Guid targetId,
        string? beforeJson = null,
        string? afterJson = null) => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ActorType = "user",
            ActorUserId = actorUserId,
            ActorLabel = null,
            LogType = "SECURITY",
            Activity = activity,
            Module = "roles",
            TargetType = "role",
            TargetId = targetId,
            BeforeJson = beforeJson,
            AfterJson = afterJson,
            OccurredAt = DateTimeOffset.UtcNow,
        };

    private static readonly JsonSerializerOptions _auditJsonOptions = new() { WriteIndented = false };

    private static string SerializeAudit(object payload) =>
        JsonSerializer.Serialize(payload, _auditJsonOptions);
}
