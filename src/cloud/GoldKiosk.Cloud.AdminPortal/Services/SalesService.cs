using System.Globalization;
using System.Text;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Tx;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Read-only service powering the Sales/PreciousMetal and Sales/PawnSales views.
/// Filters <c>tx.transactions</c> by <c>kind</c> and the requested filters.
/// </summary>
public sealed class SalesService(AppDbContext db, ICurrentUserService currentUser) : ISalesService
{
    /// <summary>List.</summary>
    public async Task<SalesClientList> ListAsync(
        string kind,
        string? search,
        string? source,
        DateTime? startDate,
        DateTime? endDate,
        bool showJunkOnly,
        bool showActiveOnly,
        int pageSize,
        int pageNo,
        CancellationToken ct = default)
    {
        // Alias the CA1716-renamed parameters so the ported body stays identical.
        var from = startDate;
        var to = endDate;
        if (currentUser.TenantId is not Guid tenantId)
        {
            return Empty(kind, pageSize, pageNo);
        }

        var baseQuery = db.Transactions.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.Kind == kind);

        var q = ApplyFilters(baseQuery, search, source, from, to, showJunkOnly, showActiveOnly);

        // Single tenant-scoped aggregation for stats — bucketise by status.
        // "Active" is anything not declined/aborted.
        var totals = await baseQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                AllClients = g.Select(t => t.CustomerId).Distinct().Count(),
                RegisteredClients = g.Where(t => t.Status == "accepted" || t.Status == "paid")
                                     .Select(t => t.CustomerId).Distinct().Count(),
                ProcessClients = g.Where(t => t.Status == "started" || t.Status == "weighing" ||
                                                  t.Status == "analysing" || t.Status == "offered")
                                     .Select(t => t.CustomerId).Distinct().Count(),
                UnpaidClients = g.Where(t => t.Status == "accepted")
                                     .Select(t => t.CustomerId).Distinct().Count(),
                ClosedClients = g.Where(t => t.Status == "declined" || t.Status == "aborted" || t.Status == "paid")
                                     .Select(t => t.CustomerId).Distinct().Count(),
            })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await q.CountAsync(ct);

        // Join customers to surface the customer_code; PII columns are not projected.
        // Two-step projection: the LEFT JOIN (DefaultIfEmpty) makes EF's shaper
        // try to coerce DateTimeOffset → Nullable<DateTime> for in-projection
        // conversions like `.UtcDateTime`, which throws. Pull the raw column
        // types from SQL, then convert on the client.
        var rows = await (
            from t in q
            join c in db.Customers.AsNoTracking() on t.CustomerId equals c.Id into cs
            from c in cs.DefaultIfEmpty()
            orderby t.OccurredAt descending
            select new
            {
                t.Id,
                t.TransactionCode,
                t.CustomerId,
                CustomerCode = c != null ? c.CustomerCode : string.Empty,
                t.Kind,
                t.Status,
                t.Source,
                t.TotalAmount,
                t.NetPayout,
                t.CurrencyCode,
                t.IsJunk,
                t.OccurredAt,
            })
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .ToListAsync(ct);

        var page = rows.Select(r => new SalesClientRowViewModel
        {
            TransactionId = r.Id,
            TransactionCode = r.TransactionCode,
            CustomerId = r.CustomerId,
            CustomerCode = r.CustomerCode,
            MobileMasked = "***",
            Kind = r.Kind,
            Status = r.Status,
            Source = r.Source,
            TotalAmount = r.TotalAmount,
            NetPayout = r.NetPayout,
            CurrencyCode = r.CurrencyCode,
            IsJunk = r.IsJunk,
            OccurredOn = r.OccurredAt.UtcDateTime,
        }).ToList();

        return new SalesClientList
        {
            Kind = kind,
            Items = page,
            AllClients = totals?.AllClients ?? 0,
            RegisteredClients = totals?.RegisteredClients ?? 0,
            ProcessClients = totals?.ProcessClients ?? 0,
            UnpaidClients = totals?.UnpaidClients ?? 0,
            ClosedClients = totals?.ClosedClients ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
            SourceDropdown = new(),
            CoordinatorDropdown = new(),
        };
    }

    /// <summary>Export csv.</summary>
    public async Task<byte[]> ExportCsvAsync(
        string kind,
        string? search,
        string? source,
        DateTime? startDate,
        DateTime? endDate,
        bool showJunkOnly,
        bool showActiveOnly,
        CancellationToken ct = default)
    {
        // Alias the CA1716-renamed parameters so the ported body stays identical.
        var from = startDate;
        var to = endDate;
        if (currentUser.TenantId is not Guid tenantId)
        {
            return Encoding.UTF8.GetBytes("﻿" + Header());
        }

        var baseQuery = db.Transactions.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.Kind == kind);

        // Two-step projection to keep EF's shaper from trying to coerce
        // DateTimeOffset → Nullable<DateTime> inside the SQL projection.
        var raw = await ApplyFilters(baseQuery, search, source, from, to, showJunkOnly, showActiveOnly)
            .OrderByDescending(t => t.OccurredAt)
            .Select(t => new
            {
                t.TransactionCode,
                t.Kind,
                t.Status,
                t.Source,
                t.TotalAmount,
                t.NetPayout,
                t.CurrencyCode,
                t.IsJunk,
                t.OccurredAt,
            })
            .ToListAsync(ct);

        var rows = raw.Select(r => new SalesClientRowViewModel
        {
            TransactionCode = r.TransactionCode,
            Kind = r.Kind,
            Status = r.Status,
            Source = r.Source,
            TotalAmount = r.TotalAmount,
            NetPayout = r.NetPayout,
            CurrencyCode = r.CurrencyCode,
            IsJunk = r.IsJunk,
            OccurredOn = r.OccurredAt.UtcDateTime,
        }).ToList();

        var sb = new StringBuilder();
        sb.Append('﻿');
        sb.AppendLine(Header());
        foreach (var r in rows)
        {
            sb.Append(Csv(r.TransactionCode)).Append(',');
            sb.Append(Csv(r.Kind)).Append(',');
            sb.Append(Csv(r.Status)).Append(',');
            sb.Append(Csv(r.Source ?? string.Empty)).Append(',');
            sb.Append(r.TotalAmount.ToString("0.##", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(r.NetPayout.ToString("0.##", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(Csv(r.CurrencyCode)).Append(',');
            sb.Append(r.IsJunk ? "Yes" : "No").Append(',');
            sb.AppendLine(r.OccurredOn.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ─── helpers ───────────────────────────────────────────────────────────
    private static IQueryable<Transaction> ApplyFilters(
        IQueryable<Transaction> q,
        string? search,
        string? source,
        DateTime? from,
        DateTime? to,
        bool showJunkOnly,
        bool showActiveOnly)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(t => EF.Functions.ILike(t.TransactionCode, s));
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            q = q.Where(t => t.Source == source);
        }

        if (from is DateTime f)
        {
            var fo = new DateTimeOffset(DateTime.SpecifyKind(f.Date, DateTimeKind.Utc));
            q = q.Where(t => t.OccurredAt >= fo);
        }

        if (to is DateTime tt)
        {
            var toExclusive = new DateTimeOffset(DateTime.SpecifyKind(tt.Date.AddDays(1), DateTimeKind.Utc));
            q = q.Where(t => t.OccurredAt < toExclusive);
        }

        if (showJunkOnly)
        {
            q = q.Where(t => t.IsJunk);
        }

        if (showActiveOnly)
        {
            q = q.Where(t => t.Status != "declined" && t.Status != "aborted");
        }

        return q;
    }

    private static SalesClientList Empty(string kind, int pageSize, int pageNo) => new()
    {
        Kind = kind,
        Items = new(),
        AllClients = 0,
        RegisteredClients = 0,
        ProcessClients = 0,
        UnpaidClients = 0,
        ClosedClients = 0,
        PageSize = pageSize,
        PageNo = pageNo,
        PageCount = 0,
        TotalPage = 0,
        SourceDropdown = new(),
        CoordinatorDropdown = new(),
    };

    private static string Header()
        => "TransactionCode,Kind,Status,Source,TotalAmount,NetPayout,Currency,IsJunk,OccurredOn";

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
