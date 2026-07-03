using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Reads from <c>monitor.exception_logs</c>. Tenant filtering rules:
/// <list type="bullet">
///   <item>A user with a tenant context sees rows where
///         <c>tenant_id = currentUser.TenantId</c> OR <c>tenant_id IS NULL</c>
///         (platform-level errors that don't carry a tenant).</item>
///   <item>A user with no tenant context (platform admin) sees every row.</item>
/// </list>
/// </summary>
public sealed class ExceptionMonitoringService(AppDbContext db, ICurrentUserService currentUser)
    : IExceptionMonitoringService
{
    /// <summary>List.</summary>
    public async Task<ExceptionLogList> ListAsync(
        string? search,
        string? severity,
        string? source,
        DateTime? startDate,
        DateTime? endDate,
        int pageSize,
        int pageNo,
        CancellationToken ct = default)
    {
        var q = db.ExceptionLogs.AsNoTracking().AsQueryable();

        if (currentUser.TenantId is Guid tenantId)
        {
            q = q.Where(e => e.TenantId == tenantId || e.TenantId == null);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(e =>
                EF.Functions.ILike(e.ExceptionName, s) ||
                EF.Functions.ILike(e.Message, s));
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            q = q.Where(e => e.Severity == severity);
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            q = q.Where(e => e.Source == source);
        }

        if (startDate is DateTime sd)
        {
            var from = new DateTimeOffset(sd.Date, TimeSpan.Zero);
            q = q.Where(e => e.OccurredAt >= from);
        }
        if (endDate is DateTime ed)
        {
            var to = new DateTimeOffset(ed.Date.AddDays(1), TimeSpan.Zero);
            q = q.Where(e => e.OccurredAt < to);
        }

        // Stats are computed against the same tenant-scoped pool (without the
        // search/severity/source filters) so the tile numbers stay stable
        // while the user filters the table.
        var statsQ = db.ExceptionLogs.AsNoTracking().AsQueryable();
        if (currentUser.TenantId is Guid tid)
        {
            statsQ = statsQ.Where(e => e.TenantId == tid || e.TenantId == null);
        }

        var totals = await statsQ
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Unresolved = g.Count(e => !e.IsResolved),
                Critical = g.Count(e => e.Severity == "critical"),
            })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await q.CountAsync(ct);

        // Two-step projection: EF Core 10 shaper cannot coerce DateTimeOffset → DateTime in-projection.
        var raw = await q
            .OrderByDescending(e => e.OccurredAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                e.ExceptionName,
                e.Message,
                e.Source,
                e.Severity,
                e.OccurredAt,
                e.IsResolved,
                e.ResolvedAt,
                e.TenantId,
                e.KioskId,
                e.CorrelationId,
            })
            .ToListAsync(ct);

        var items = raw.Select(e => new ExceptionLogViewModel
        {
            Id = e.Id,
            ExceptionName = e.ExceptionName,
            Message = e.Message,
            Source = e.Source,
            Severity = e.Severity,
            OccurredAt = e.OccurredAt.UtcDateTime,
            IsResolved = e.IsResolved,
            ResolvedAt = e.ResolvedAt == null ? (DateTime?)null : e.ResolvedAt.Value.UtcDateTime,
            TenantId = e.TenantId,
            KioskId = e.KioskId,
            CorrelationId = e.CorrelationId,
        }).ToList();

        return new ExceptionLogList
        {
            Items = items,
            Total = totals?.Total ?? 0,
            Unresolved = totals?.Unresolved ?? 0,
            Critical = totals?.Critical ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    /// <summary>Mark resolved.</summary>
    public async Task<OperationResult> MarkResolvedAsync(Guid id, CancellationToken ct = default)
    {
        var row = await db.ExceptionLogs.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (row is null)
        {
            return OperationResult.Fail("Exception log entry not found.");
        }

        // Defence-in-depth: a tenant user may only resolve rows they can see.
        if (currentUser.TenantId is Guid tenantId
            && row.TenantId is Guid rowTenant
            && rowTenant != tenantId)
        {
            return OperationResult.Fail("Not permitted.");
        }

        if (row.IsResolved)
        {
            return OperationResult.Ok();
        }

        row.IsResolved = true;
        row.ResolvedAt = DateTimeOffset.UtcNow;
        row.ResolvedByUserId = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }
}
