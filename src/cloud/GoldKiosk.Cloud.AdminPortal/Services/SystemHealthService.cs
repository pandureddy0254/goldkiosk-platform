using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>System health service.</summary>
public sealed class SystemHealthService(AppDbContext db, ICurrentUserService currentUser) : ISystemHealthService
{
    /// <summary>Get dashboard.</summary>
    public async Task<SystemHealthDashboardViewModel> GetDashboardAsync(CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return new SystemHealthDashboardViewModel();
        }

        var since = DateTimeOffset.UtcNow.AddHours(-24);

        var rows = await db.SystemHealthSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.CapturedAt >= since)
            .OrderByDescending(s => s.CapturedAt)
            .Select(s => new SystemHealthSnapshotViewModel
            {
                Id = s.Id,
                CapturedAt = s.CapturedAt,
                TotalCities = s.TotalCities,
                TotalKiosks = s.TotalKiosks,
                FunctionalCount = s.FunctionalCount,
                NonFunctionalCount = s.NonFunctionalCount,
                AvgUptimePct = s.AvgUptimePct,
            })
            .ToListAsync(ct);

        return new SystemHealthDashboardViewModel
        {
            Latest = rows.FirstOrDefault(),
            Trend24h = rows,
        };
    }
}
