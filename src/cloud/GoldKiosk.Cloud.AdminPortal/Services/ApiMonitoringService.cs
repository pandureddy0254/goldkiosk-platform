using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Aggregates <c>monitor.api_request_logs</c> by endpoint into tile + table data
/// for the API Health Monitoring view, and joins the latest
/// <c>monitor.api_health_checks</c> row per endpoint for health status.
/// <para>
/// Tenant filtering: tenant users see only requests that carry their tenant
/// id; platform admins (no tenant context) see everything.
/// </para>
/// </summary>
public sealed class ApiMonitoringService(AppDbContext db, ICurrentUserService currentUser)
    : IApiMonitoringService
{
    /// <summary>Get stats.</summary>
    public async Task<ApiHealthStatsViewModel> GetStatsAsync(
        string? search,
        int pageSize,
        int pageNo,
        CancellationToken ct = default)
    {
        var q = db.ApiRequestLogs.AsNoTracking().AsQueryable();
        if (currentUser.TenantId is Guid tenantId)
        {
            q = q.Where(r => r.TenantId == tenantId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(r => EF.Functions.ILike(r.Endpoint, s));
        }

        // ─── Per-endpoint breakdown ─────────────────────────────────────────
        var grouped = q
            .GroupBy(r => r.Endpoint)
            .Select(g => new
            {
                Endpoint = g.Key,
                Total = g.Count(),
                SuccessCount = g.Count(r => r.IsSuccess && r.StatusCode >= 200 && r.StatusCode < 300),
                FailedCount = g.Count(r => r.StatusCode >= 400 && r.StatusCode < 600),
                PendingCount = g.Count(r => r.StatusCode >= 100 && r.StatusCode < 200),
                OthersCount = g.Count(r =>
                    !(r.IsSuccess && r.StatusCode >= 200 && r.StatusCode < 300) &&
                    !(r.StatusCode >= 400 && r.StatusCode < 600) &&
                    !(r.StatusCode >= 100 && r.StatusCode < 200)),
            });

        var totalEndpoints = await grouped.CountAsync(ct);

        var pageRows = await grouped
            .OrderByDescending(x => x.Total)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .ToListAsync(ct);

        // Pull the latest health-check row per endpoint shown on the current page.
        var endpointNames = pageRows.Select(r => r.Endpoint).ToList();
        var latestHealth = await db.ApiHealthChecks.AsNoTracking()
            .Where(h => endpointNames.Contains(h.Endpoint))
            .GroupBy(h => h.Endpoint)
            .Select(g => g.OrderByDescending(h => h.CheckedAt).First())
            .ToListAsync(ct);

        var endpoints = pageRows.Select(r =>
        {
            var hc = latestHealth.FirstOrDefault(h => h.Endpoint == r.Endpoint);
            return new ApiHealthEndpointViewModel
            {
                Endpoint = r.Endpoint,
                RequestCount = r.Total,
                SuccessPct = Pct(r.SuccessCount, r.Total),
                FailedPct = Pct(r.FailedCount, r.Total),
                PendingPct = Pct(r.PendingCount, r.Total),
                OthersPct = Pct(r.OthersCount, r.Total),
                LastCheckedAt = hc?.CheckedAt.UtcDateTime,
                LastIsHealthy = hc?.IsHealthy,
            };
        }).ToList();

        // ─── Headline tiles ─────────────────────────────────────────────────
        // Headline tiles are computed across the unfiltered tenant pool so the
        // numbers don't change when the user searches.
        var globalQ = db.ApiRequestLogs.AsNoTracking().AsQueryable();
        if (currentUser.TenantId is Guid tid)
        {
            globalQ = globalQ.Where(r => r.TenantId == tid);
        }

        var totals = await globalQ
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.LongCount(),
                Success = g.Count(r => r.IsSuccess && r.StatusCode >= 200 && r.StatusCode < 300),
                Failed = g.Count(r => r.StatusCode >= 400 && r.StatusCode < 600),
                Pending = g.Count(r => r.StatusCode >= 100 && r.StatusCode < 200),
                Others = g.Count(r =>
                    !(r.IsSuccess && r.StatusCode >= 200 && r.StatusCode < 300) &&
                    !(r.StatusCode >= 400 && r.StatusCode < 600) &&
                    !(r.StatusCode >= 100 && r.StatusCode < 200)),
            })
            .FirstOrDefaultAsync(ct);

        var totalApi = await globalQ.Select(r => r.Endpoint).Distinct().CountAsync(ct);

        var totalRequests = totals?.Total ?? 0;
        return new ApiHealthStatsViewModel
        {
            TotalApi = totalApi,
            TotalRequestCount = totalRequests,
            SuccessPct = Pct(totals?.Success ?? 0, totalRequests),
            FailedPct = Pct(totals?.Failed ?? 0, totalRequests),
            PendingPct = Pct(totals?.Pending ?? 0, totalRequests),
            OthersPct = Pct(totals?.Others ?? 0, totalRequests),
            Endpoints = endpoints,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalEndpoints,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalEndpoints / (double)pageSize) : 0,
        };
    }

    private static decimal Pct(int part, int total)
        => total <= 0 ? 0m : Math.Round((decimal)part * 100m / total, 2);

    private static decimal Pct(int part, long total)
        => total <= 0 ? 0m : Math.Round((decimal)part * 100m / total, 2);
}
