using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Access control service.</summary>
public sealed class AccessControlService(AppDbContext db, ICurrentUserService currentUser) : IAccessControlService
{
    // Verbs map permission codes to the matrix columns.
    private static readonly string[] Verbs = ["view", "add", "edit", "delete", "other"];

    /// <summary>List.</summary>
    public async Task<AccessControlPageViewModel> ListAsync(string? roleCode, string? search, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return new AccessControlPageViewModel { PageNo = pageNo, PageSize = pageSize };
        }

        var modulesQ = db.Modules.AsNoTracking().Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            modulesQ = modulesQ.Where(m =>
                EF.Functions.ILike(m.ModuleName, s) ||
                EF.Functions.ILike(m.Controller, s) ||
                EF.Functions.ILike(m.Action, s));
        }

        var totalRecords = await modulesQ.CountAsync(ct);

        var pagedModules = await modulesQ
            .OrderBy(m => m.ModuleName).ThenBy(m => m.Controller).ThenBy(m => m.Action)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .ToListAsync(ct);

        // Resolve role + granted permission codes for that role.
        HashSet<string> grantedCodes = new(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            var role = await db.Roles.AsNoTracking()
                .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Code == roleCode && r.DeletedAt == null, ct);
            if (role is not null)
            {
                grantedCodes = (await (
                    from rp in db.RolePermissions.AsNoTracking()
                    join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                    where rp.RoleId == role.Id
                    select p.Code
                ).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
        }

        var items = pagedModules.Select(m =>
        {
            var prefix = m.Controller.ToLowerInvariant();
            return new AccessModuleViewModel
            {
                Code = $"{m.Controller}.{m.Action}",
                ControllerName = m.Controller,
                ActionName = m.Action,
                ModuleName = m.ModuleName,
                SubModuleName = m.SubModule ?? string.Empty,
                SubSubModuleName = m.SubSubModule ?? string.Empty,
                Type = m.Type,
                IsActive = m.IsActive,
                RoleCode = roleCode ?? string.Empty,
                AvailableTypes = new HashSet<string>(Verbs),
                IsView = grantedCodes.Contains($"{prefix}.view"),
                IsAdd = grantedCodes.Contains($"{prefix}.add"),
                IsEdit = grantedCodes.Contains($"{prefix}.edit"),
                IsDelete = grantedCodes.Contains($"{prefix}.delete"),
                IsOther = grantedCodes.Contains($"{prefix}.other"),
            };
        }).ToList();

        return new AccessControlPageViewModel
        {
            Items = items,
            RoleCode = roleCode ?? string.Empty,
            Search = search ?? string.Empty,
            PageNo = pageNo,
            PageSize = pageSize,
            TotalRecords = totalRecords,
        };
    }

    /// <summary>Update.</summary>
    public async Task<OperationResult> UpdateAsync(AccessControlPageViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.RoleCode))
        {
            return OperationResult.Fail("Role is required.");
        }

        var role = await db.Roles.FirstOrDefaultAsync(r =>
            r.TenantId == tenantId && r.Code == vm.RoleCode && r.DeletedAt == null, ct);
        if (role is null)
        {
            return OperationResult.Fail($"Role '{vm.RoleCode}' not found.");
        }

        if (role.IsSystem)
        {
            return OperationResult.Fail("System roles cannot be modified.");
        }

        foreach (var item in vm.Items ?? new List<AccessModuleViewModel>())
        {
            await ApplyMatrixAsync(role.Id, item, ct);
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Update single.</summary>
    public async Task<OperationResult> UpdateSingleAsync(AccessModuleViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.RoleCode))
        {
            return OperationResult.Fail("Role is required.");
        }

        var role = await db.Roles.FirstOrDefaultAsync(r =>
            r.TenantId == tenantId && r.Code == vm.RoleCode && r.DeletedAt == null, ct);
        if (role is null)
        {
            return OperationResult.Fail($"Role '{vm.RoleCode}' not found.");
        }

        if (role.IsSystem)
        {
            return OperationResult.Fail("System roles cannot be modified.");
        }

        await ApplyMatrixAsync(role.Id, vm, ct);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    // ─── helpers ───────────────────────────────────────────────────────────────
    private async Task ApplyMatrixAsync(Guid roleId, AccessModuleViewModel item, CancellationToken ct)
    {
        var prefix = string.IsNullOrWhiteSpace(item.ControllerName)
            ? (item.Code.Contains('.') ? item.Code.Split('.', 2)[0] : item.Code)
            : item.ControllerName;
        prefix = prefix.ToLowerInvariant();

        var desired = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            [$"{prefix}.view"] = item.IsView,
            [$"{prefix}.add"] = item.IsAdd,
            [$"{prefix}.edit"] = item.IsEdit,
            [$"{prefix}.delete"] = item.IsDelete,
            [$"{prefix}.other"] = item.IsOther,
        };

        var codes = desired.Keys.ToList();
        var permissions = await db.Permissions
            .Where(p => codes.Contains(p.Code))
            .ToListAsync(ct);
        var permissionsByCode = permissions.ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var (code, shouldGrant) in desired)
        {
            if (!permissionsByCode.TryGetValue(code, out var perm))
            {
                // Auto-create the permission if missing — keeps the matrix actionable
                // without a pre-seeded catalog. The scope defaults to 'tenant'.
                if (!shouldGrant)
                {
                    continue;
                }

                perm = new AppPermission
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    DisplayName = code,
                    Scope = "tenant",
                };
                db.Permissions.Add(perm);
                permissionsByCode[code] = perm;
            }

            var existing = await db.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == perm.Id, ct);

            switch (existing, shouldGrant)
            {
                case (null, true):
                    db.RolePermissions.Add(new AppRolePermission
                    {
                        RoleId = roleId,
                        PermissionId = perm.Id,
                        GrantedAt = DateTimeOffset.UtcNow,
                    });
                    break;
                case ({ } row, false):
                    db.RolePermissions.Remove(row);
                    break;
            }
        }
    }
}
