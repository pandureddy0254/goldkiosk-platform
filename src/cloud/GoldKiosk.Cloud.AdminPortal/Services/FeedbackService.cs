using System.Globalization;
using System.Text;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Helpdesk;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Feedback service.</summary>
public sealed class FeedbackService(AppDbContext db, ICurrentUserService currentUser) : IFeedbackService
{
    /// <summary>List.</summary>
    public async Task<FeedbackMasterVM> ListAsync(
        string? search,
        string? stat,
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

        var q = ApplyFilters(db.Feedback.AsNoTracking().Where(f => f.TenantId == tenantId), search, stat, startDate, endDate);

        var totals = await db.Feedback.AsNoTracking()
            .Where(f => f.TenantId == tenantId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Read = g.Count(f => f.IsRead),
                Unread = g.Count(f => !f.IsRead)
            })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(f => f.CreatedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(f => Map(f))
            .ToListAsync(ct);

        return new FeedbackMasterVM
        {
            FeedbackItems = items,
            Total = totals?.Total ?? 0,
            Read = totals?.Read ?? 0,
            Unread = totals?.Unread ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    /// <summary>Mark as read.</summary>
    public async Task<OperationResult> MarkAsReadAsync(Guid id, CancellationToken ct = default)
        => await SetReadFlagAsync(id, true, ct);

    /// <summary>Mark as unread.</summary>
    public async Task<OperationResult> MarkAsUnreadAsync(Guid id, CancellationToken ct = default)
        => await SetReadFlagAsync(id, false, ct);

    /// <summary>Export csv.</summary>
    public async Task<byte[]> ExportCsvAsync(
        string? search,
        string? stat,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return Encoding.UTF8.GetBytes("﻿" + Header());
        }

        var rows = await ApplyFilters(
                db.Feedback.AsNoTracking().Where(f => f.TenantId == tenantId),
                search, stat, startDate, endDate)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => Map(f))
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.Append('﻿'); // UTF-8 BOM so Excel auto-detects encoding
        sb.AppendLine(Header());
        foreach (var r in rows)
        {
            sb.Append(Csv(r.Type)).Append(',');
            sb.Append(Csv(r.FunctionName)).Append(',');
            sb.Append(Csv(r.Description ?? string.Empty)).Append(',');
            sb.Append(Csv(r.Location ?? string.Empty)).Append(',');
            sb.Append(r.Isread ? "Read" : "Unread").Append(',');
            sb.Append(r.CreatedOn.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(Csv(r.AudioUrl ?? string.Empty)).Append(',');
            sb.AppendLine(Csv(r.ImgUrl ?? string.Empty));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ─── helpers ───────────────────────────────────────────────────────────
    private async Task<OperationResult> SetReadFlagAsync(Guid id, bool isRead, CancellationToken ct)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var f = await db.Feedback.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (f is null)
        {
            return OperationResult.Fail("Feedback not found.");
        }

        f.IsRead = isRead;
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    private static IQueryable<Feedback> ApplyFilters(
        IQueryable<Feedback> q,
        string? search,
        string? stat,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(f =>
                EF.Functions.ILike(f.FunctionName, s) ||
                (f.Description != null && EF.Functions.ILike(f.Description, s)) ||
                EF.Functions.ILike(f.FeedbackType, s));
        }

        if (!string.IsNullOrWhiteSpace(stat) && bool.TryParse(stat, out var isRead))
        {
            q = q.Where(f => f.IsRead == isRead);
        }

        if (startDate is DateTime sd)
        {
            var sdo = new DateTimeOffset(DateTime.SpecifyKind(sd.Date, DateTimeKind.Utc));
            q = q.Where(f => f.CreatedAt >= sdo);
        }

        if (endDate is DateTime ed)
        {
            var edo = new DateTimeOffset(DateTime.SpecifyKind(ed.Date.AddDays(1), DateTimeKind.Utc));
            q = q.Where(f => f.CreatedAt < edo);
        }

        return q;
    }

    private static FeedbackListViewModel Map(Feedback f) => new()
    {
        Code = f.Id.ToString(),
        FunctionName = f.FunctionName,
        Type = f.FeedbackType,
        Description = f.Description,
        AudioUrl = f.AudioUrl,
        ImgUrl = f.ImageUrl,
        Location = f.LocationText,
        Isread = f.IsRead,
        CreatedOn = f.CreatedAt.UtcDateTime,
    };

    private static FeedbackMasterVM Empty(int pageSize, int pageNo) => new()
    {
        FeedbackItems = new List<FeedbackListViewModel>(),
        Total = 0,
        Read = 0,
        Unread = 0,
        PageSize = pageSize,
        PageNo = pageNo,
        PageCount = 0,
        TotalPage = 0,
    };

    private static string Header()
        => "Type,Function,Description,Location,Status,CreatedOn,AudioUrl,ImageUrl";

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
