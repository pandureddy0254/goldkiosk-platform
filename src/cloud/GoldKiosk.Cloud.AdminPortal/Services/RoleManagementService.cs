using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Role management service.</summary>
public sealed class RoleManagementService(AppDbContext db, ICurrentUserService currentUser) : IRoleManagementService
{
    /// <summary>List.</summary>
    public async Task<RoleMasterList> ListAsync(string? search, string? stat, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return EmptyResult(pageSize, pageNo);
        }

        var q = db.Roles.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(r => EF.Functions.ILike(r.Code, s) || EF.Functions.ILike(r.Name, s));
        }

        if (!string.IsNullOrWhiteSpace(stat) && bool.TryParse(stat, out var active))
        {
            q = q.Where(r => r.IsActive == active);
        }

        var totals = await db.Roles.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.DeletedAt == null)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(r => r.IsActive),
                InActive = g.Count(r => !r.IsActive)
            })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await q.CountAsync(ct);

        // Two-step projection: EF shaper can't coerce DateTimeOffset → DateTime in SQL.
        var rawRoles = await q
            .OrderByDescending(r => r.CreatedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(r => new { r.Code, r.Name, r.TenantId, r.IsActive, r.CreatedAt })
            .ToListAsync(ct);

        var items = rawRoles.Select(r => new RoleMasterViewModel
        {
            Code = r.Code,
            Name = r.Name,
            ClientCode = r.TenantId.ToString(),
            IsActive = r.IsActive,
            CreatedOn = r.CreatedAt.UtcDateTime,
        }).ToList();

        return new RoleMasterList
        {
            roleMasterList = items,
            Total = totals?.Total ?? 0,
            Active = totals?.Active ?? 0,
            InActive = totals?.InActive ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    /// <summary>Get by code.</summary>
    public async Task<RoleMasterViewModel?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return null;
        }

        var r = await db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code && x.DeletedAt == null, ct);

        return r is null ? null : Map(r);
    }

    /// <summary>Add.</summary>
    public async Task<OperationResult> AddAsync(RoleMasterViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.Code) || string.IsNullOrWhiteSpace(vm.Name))
        {
            return OperationResult.Fail("Role code and name are required.");
        }

        var duplicate = await db.Roles.AnyAsync(r =>
            r.TenantId == tenantId && r.Code == vm.Code && r.DeletedAt == null, ct);
        if (duplicate)
        {
            return OperationResult.Fail($"Role with code '{vm.Code}' already exists.");
        }

        var role = new AppRole
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = vm.Code!,
            Name = vm.Name!,
            IsSystem = false,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit.</summary>
    public async Task<OperationResult> EditAsync(RoleMasterViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.Code))
        {
            return OperationResult.Fail("Role code is required.");
        }

        var role = await db.Roles
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Code == vm.Code && r.DeletedAt == null, ct);
        if (role is null)
        {
            return OperationResult.Fail($"Role '{vm.Code}' not found.");
        }

        if (role.IsSystem)
        {
            return OperationResult.Fail("System roles cannot be modified.");
        }

        if (!string.IsNullOrWhiteSpace(vm.Name))
        {
            role.Name = vm.Name!;
        }

        role.IsActive = vm.IsActive;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Delete.</summary>
    public async Task<OperationResult> DeleteAsync(string code, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return OperationResult.Fail("Role code is required.");
        }

        var role = await db.Roles
            .FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Code == code && r.DeletedAt == null, ct);
        if (role is null)
        {
            return OperationResult.Fail($"Role '{code}' not found.");
        }

        if (role.IsSystem)
        {
            return OperationResult.Fail("System roles cannot be deleted.");
        }

        role.DeletedAt = DateTimeOffset.UtcNow;
        role.IsActive = false;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    // ─── helpers ───────────────────────────────────────────────────────────────
    private static RoleMasterViewModel Map(AppRole r) => new()
    {
        Code = r.Code,
        Name = r.Name,
        ClientCode = r.TenantId.ToString(),
        IsActive = r.IsActive,
        CreatedOn = r.CreatedAt.UtcDateTime,
    };

    private static RoleMasterList EmptyResult(int pageSize, int pageNo) => new()
    {
        roleMasterList = new List<RoleMasterViewModel>(),
        Total = 0,
        Active = 0,
        InActive = 0,
        PageSize = pageSize,
        PageNo = pageNo,
        PageCount = 0,
        TotalPage = 0,
    };
}
