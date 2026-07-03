using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Ops;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Technician service.</summary>
public sealed class TechnicianService(AppDbContext db, ICurrentUserService currentUser) : ITechnicianService
{
    private static readonly string[] AllowedStatuses = ["active", "on_leave", "inactive"];

    /// <summary>List.</summary>
    public async Task<TechnicianList> ListAsync(string? search, Guid? clusterId, string? status, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return EmptyResult(pageSize, pageNo);
        }

        var q =
            from t in db.Technicians.AsNoTracking().Where(x => x.TenantId == tenantId)
            join c in db.TechnicianClusters.AsNoTracking() on t.ClusterId equals c.Id
            select new { t, ClusterCode = c.Code, ClusterName = c.Name };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.t.Name, s));
        }
        if (clusterId is { } cid)
        {
            q = q.Where(x => x.t.ClusterId == cid);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            q = q.Where(x => x.t.Status == status);
        }

        var totals = await db.Technicians.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(t => t.IsActive),
                InActive = g.Count(t => !t.IsActive),
            })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await q.CountAsync(ct);

        // Two-step projection: shaper cannot coerce DateTimeOffset -> Nullable<DateTime> inside a JOIN shape.
        var raw = await q
            .OrderByDescending(x => x.t.CreatedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(x => new
            {
                x.t.Id,
                x.t.Name,
                x.t.ClusterId,
                x.ClusterCode,
                x.ClusterName,
                x.t.Status,
                x.t.IsActive,
                x.t.CreatedAt,
            })
            .ToListAsync(ct);

        var items = raw.Select(x => new TechnicianViewModel
        {
            Id = x.Id,
            Name = x.Name,
            ClusterId = x.ClusterId,
            ClusterCode = x.ClusterCode,
            ClusterName = x.ClusterName,
            Status = x.Status,
            IsActive = x.IsActive,
            CreatedOn = x.CreatedAt.UtcDateTime,
        }).ToList();

        var clusters = await ListClustersAsync(ct);

        return new TechnicianList
        {
            Items = items,
            Clusters = clusters.ToList(),
            Total = totals?.Total ?? 0,
            Active = totals?.Active ?? 0,
            InActive = totals?.InActive ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    /// <summary>List clusters.</summary>
    public async Task<IReadOnlyList<TechnicianClusterViewModel>> ListClustersAsync(CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return Array.Empty<TechnicianClusterViewModel>();
        }

        return await db.TechnicianClusters.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .OrderBy(c => c.Code)
            .Select(c => new TechnicianClusterViewModel
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                IsActive = c.IsActive,
            })
            .ToListAsync(ct);
    }

    /// <summary>Add.</summary>
    public async Task<OperationResult> AddAsync(TechnicianViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.Name))
        {
            return OperationResult.Fail("Name is required.");
        }

        if (vm.ClusterId == default)
        {
            return OperationResult.Fail("Cluster is required.");
        }

        if (!AllowedStatuses.Contains(vm.Status))
        {
            vm.Status = "active";
        }

        var t = new Technician
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = vm.Name,
            ClusterId = vm.ClusterId,
            Status = vm.Status,
            IsActive = vm.IsActive,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Technicians.Add(t);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit.</summary>
    public async Task<OperationResult> EditAsync(TechnicianViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var t = await db.Technicians.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == vm.Id, ct);
        if (t is null)
        {
            return OperationResult.Fail("Technician not found.");
        }

        if (!string.IsNullOrWhiteSpace(vm.Name))
        {
            t.Name = vm.Name;
        }

        if (vm.ClusterId != default)
        {
            t.ClusterId = vm.ClusterId;
        }

        if (!string.IsNullOrWhiteSpace(vm.Status) && AllowedStatuses.Contains(vm.Status))
        {
            t.Status = vm.Status;
        }

        t.IsActive = vm.IsActive;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Delete.</summary>
    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var t = await db.Technicians.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        if (t is null)
        {
            return OperationResult.Fail("Technician not found.");
        }

        // Soft-delete: mark inactive rather than removing referential history.
        t.IsActive = false;
        t.Status = "inactive";

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    private static TechnicianList EmptyResult(int pageSize, int pageNo) => new()
    {
        Items = new List<TechnicianViewModel>(),
        Clusters = new List<TechnicianClusterViewModel>(),
        PageSize = pageSize,
        PageNo = pageNo,
    };
}
