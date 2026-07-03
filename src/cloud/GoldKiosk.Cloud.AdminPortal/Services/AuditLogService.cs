using System.Globalization;
using System.Text;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Audit;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Audit log service.</summary>
public sealed class AuditLogService(AppDbContext db, ICurrentUserService currentUser) : IAuditLogService
{
    /// <summary>List.</summary>
    public async Task<UserActivityList> ListAsync(
        string? search,
        string? feature,
        string? type,
        string? status,
        string? category,
        string? subCategory,
        DateTime? startDate,
        DateTime? endDate,
        int pageSize,
        int pageNo,
        CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return Empty(pageSize, pageNo);
        }

        var q = ApplyFilters(
            db.AuditEvents.AsNoTracking().Where(a => a.TenantId == tenantId),
            search, feature, type, status, category, subCategory, startDate, endDate);

        var total = await q.CountAsync(ct);

        // Two-step projection: EF's shaper can't coerce DateTimeOffset → DateTime in SQL.
        var rawPage = await q
            .OrderByDescending(a => a.OccurredAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(a => new
            {
                a.LogType,
                a.Activity,
                a.Module,
                a.SubModule,
                a.SubSubModule,
                a.ActorLabel,
                a.ActorType,
                a.OccurredAt,
            })
            .ToListAsync(ct);

        var items = rawPage.Select(a => new UserActivity
        {
            LogType = a.LogType,
            Activity = a.Activity,
            Module = a.Module ?? string.Empty,
            SubModule = a.SubModule ?? string.Empty,
            SubSubModule = a.SubSubModule ?? string.Empty,
            UpdatedBy = a.ActorLabel ?? a.ActorType,
            UpdatedOn = a.OccurredAt.UtcDateTime,
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

    /// <summary>Export csv.</summary>
    public async Task<byte[]> ExportCsvAsync(
        string? search,
        string? feature,
        string? type,
        string? status,
        string? category,
        string? subCategory,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return Encoding.UTF8.GetBytes("﻿" + Header());
        }

        var rawRows = await ApplyFilters(
                db.AuditEvents.AsNoTracking().Where(a => a.TenantId == tenantId),
                search, feature, type, status, category, subCategory, startDate, endDate)
            .OrderByDescending(a => a.OccurredAt)
            .Select(a => new
            {
                a.LogType,
                a.Activity,
                a.Module,
                a.SubModule,
                a.SubSubModule,
                a.ActorLabel,
                a.ActorType,
                a.OccurredAt,
            })
            .ToListAsync(ct);

        var rows = rawRows.Select(a => new UserActivity
        {
            LogType = a.LogType,
            Activity = a.Activity,
            Module = a.Module ?? string.Empty,
            SubModule = a.SubModule ?? string.Empty,
            SubSubModule = a.SubSubModule ?? string.Empty,
            UpdatedBy = a.ActorLabel ?? a.ActorType,
            UpdatedOn = a.OccurredAt.UtcDateTime,
        }).ToList();

        var sb = new StringBuilder();
        sb.Append('﻿'); // UTF-8 BOM
        sb.AppendLine(Header());
        foreach (var r in rows)
        {
            sb.Append(Csv(r.LogType)).Append(',');
            sb.Append(Csv(r.Activity)).Append(',');
            sb.Append(Csv(r.Module)).Append(',');
            sb.Append(Csv(r.SubModule)).Append(',');
            sb.Append(Csv(r.SubSubModule)).Append(',');
            sb.Append(Csv(r.UpdatedBy ?? string.Empty)).Append(',');
            sb.AppendLine(r.UpdatedOn.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ─── helpers ───────────────────────────────────────────────────────────
    private static IQueryable<AuditEvent> ApplyFilters(
        IQueryable<AuditEvent> q,
        string? search,
        string? feature,
        string? type,
        string? status,
        string? category,
        string? subCategory,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(a =>
                EF.Functions.ILike(a.Activity, s) ||
                (a.Module != null && EF.Functions.ILike(a.Module, s)) ||
                (a.SubModule != null && EF.Functions.ILike(a.SubModule, s)) ||
                (a.SubSubModule != null && EF.Functions.ILike(a.SubSubModule, s)) ||
                (a.ActorLabel != null && EF.Functions.ILike(a.ActorLabel, s)));
        }

        if (!string.IsNullOrWhiteSpace(feature))
        {
            q = q.Where(a => a.Module == feature);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            q = q.Where(a => a.ActorType == type);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            q = q.Where(a => a.LogType == status);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            q = q.Where(a => a.SubModule == category);
        }

        if (!string.IsNullOrWhiteSpace(subCategory))
        {
            q = q.Where(a => a.SubSubModule == subCategory);
        }

        if (startDate is DateTime sd)
        {
            var sdo = new DateTimeOffset(DateTime.SpecifyKind(sd.Date, DateTimeKind.Utc));
            q = q.Where(a => a.OccurredAt >= sdo);
        }

        if (endDate is DateTime ed)
        {
            var edo = new DateTimeOffset(DateTime.SpecifyKind(ed.Date.AddDays(1), DateTimeKind.Utc));
            q = q.Where(a => a.OccurredAt < edo);
        }

        return q;
    }

    private static UserActivity Map(AuditEvent a) => new()
    {
        LogType = a.LogType,
        Activity = a.Activity,
        Module = a.Module ?? string.Empty,
        SubModule = a.SubModule ?? string.Empty,
        SubSubModule = a.SubSubModule ?? string.Empty,
        UpdatedBy = a.ActorLabel ?? a.ActorType,
        UpdatedOn = a.OccurredAt.UtcDateTime,
    };

    private static UserActivityList Empty(int pageSize, int pageNo) => new()
    {
        ActivityList = new List<UserActivity>(),
        Total = 0,
        PageSize = pageSize,
        PageNo = pageNo,
        TotalPage = 0,
    };

    private static string Header()
        => "LogType,Activity,Module,SubModule,SubSubModule,UpdatedBy,UpdatedOn";

    private static string Csv(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return string.Empty;
        }

        var needsQuotes = s.IndexOfAny([',', '"', '\r', '\n']) >= 0;
        var escaped = s.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }
}
