using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Stub read of <c>ops.kiosk_inventory_snapshots</c> for the
/// <c>PreciousMetalMonitoring</c> view. Inner-join with <c>kiosk.kiosks</c>
/// gives tenant scoping (the snapshots table itself has no <c>tenant_id</c>).
/// Full reporting (aggregations, charts, exports) lands in the Reports
/// controller pass — owned by a different agent.
/// </summary>
public sealed class KioskInventoryMonitoringService(AppDbContext db, ICurrentUserService currentUser)
    : IKioskInventoryMonitoringService
{
    /// <summary>List.</summary>
    public async Task<KioskInventorySnapshotList> ListAsync(
        string? search,
        string? metal,
        int pageSize,
        int pageNo,
        CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return EmptyResult(pageSize, pageNo);
        }

        var q = from snap in db.KioskInventorySnapshots.AsNoTracking()
                join k in db.Kiosks.AsNoTracking() on snap.KioskId equals k.Id
                where k.TenantId == tenantId
                select new { snap, k.Code };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.Code, s));
        }

        if (!string.IsNullOrWhiteSpace(metal))
        {
            q = q.Where(x => x.snap.Metal == metal);
        }

        var total = await q.CountAsync(ct);

        // Two-step projection: shaper cannot coerce DateTimeOffset -> Nullable<DateTime> inside a JOIN shape.
        var raw = await q
            .OrderByDescending(x => x.snap.CapturedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(x => new
            {
                x.snap.Id,
                x.snap.KioskId,
                KioskCode = x.Code,
                x.snap.Metal,
                x.snap.WeightG,
                x.snap.Carat,
                x.snap.CapturedAt,
            })
            .ToListAsync(ct);

        var items = raw.Select(x => new KioskInventorySnapshotViewModel
        {
            Id = x.Id,
            KioskId = x.KioskId,
            KioskCode = x.KioskCode,
            Metal = x.Metal,
            WeightG = x.WeightG,
            Carat = x.Carat,
            CapturedAt = x.CapturedAt.UtcDateTime,
        }).ToList();

        return new KioskInventorySnapshotList
        {
            Items = items,
            Total = total,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = total,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0,
        };
    }

    private static KioskInventorySnapshotList EmptyResult(int pageSize, int pageNo) => new()
    {
        Items = new List<KioskInventorySnapshotViewModel>(),
        Total = 0,
        PageSize = pageSize,
        PageNo = pageNo,
        PageCount = 0,
        TotalPage = 0,
    };
}
