using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Read-only projection of <c>audit.audit_events</c> into the existing
/// <see cref="UserActivity"/> / <see cref="UserActivityList"/> ViewModels.
/// The table is append-only and owned by another agent (Feedback + AuditLogs);
/// this service only reads.
/// </summary>
public sealed class UserActivityLogService(AppDbContext db, ICurrentUserService currentUser)
    : IUserActivityLogService
{
    /// <summary>List.</summary>
    public async Task<UserActivityList> ListAsync(
        string? search,
        string? logType,
        DateTime? startDate,
        DateTime? endDate,
        int pageSize,
        int pageNo,
        CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return EmptyResult(pageSize, pageNo);
        }

        var q = db.AuditEvents.AsNoTracking().Where(e => e.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(e =>
                EF.Functions.ILike(e.Activity, s) ||
                (e.Module != null && EF.Functions.ILike(e.Module, s)) ||
                (e.ActorLabel != null && EF.Functions.ILike(e.ActorLabel, s)));
        }

        if (!string.IsNullOrWhiteSpace(logType))
        {
            q = q.Where(e => e.LogType == logType);
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

        var total = await q.CountAsync(ct);

        // Two-step projection: EF Core 10 shaper cannot coerce DateTimeOffset → DateTime in-projection.
        var raw = await q
            .OrderByDescending(e => e.OccurredAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(e => new
            {
                e.LogType,
                e.Activity,
                e.Module,
                e.SubModule,
                e.SubSubModule,
                e.ActorLabel,
                e.OccurredAt,
            })
            .ToListAsync(ct);

        var items = raw.Select(e => new UserActivity
        {
            LogType = e.LogType,
            Activity = e.Activity,
            Module = e.Module ?? string.Empty,
            SubModule = e.SubModule ?? string.Empty,
            SubSubModule = e.SubSubModule ?? string.Empty,
            UpdatedBy = e.ActorLabel ?? string.Empty,
            UpdatedOn = e.OccurredAt.UtcDateTime,
        }).ToList();

        return new UserActivityList
        {
            ActivityList = items,
            Total = total,
            PageSize = pageSize,
            PageNo = pageNo,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0,
        };
    }

    private static UserActivityList EmptyResult(int pageSize, int pageNo) => new()
    {
        ActivityList = new List<UserActivity>(),
        Total = 0,
        PageSize = pageSize,
        PageNo = pageNo,
        TotalPage = 0,
    };
}
