using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Dashboard service.</summary>
public sealed class DashboardService(AppDbContext db, ICurrentUserService currentUser) : IDashboardService
{
    // Use List<string>, not string[] / collection-expression. .NET 10's
    // string[].Contains binds through MemoryExtensions.Contains<T>(ReadOnlySpan<T>, T),
    // which the EF Core funcletizer cannot evaluate ("ReadOnlySpan violates
    // type-parameter constraint" TypeLoadException). List<T>.Contains is a
    // plain instance method and translates cleanly.
    private static readonly List<string> PaidStatuses = new() { "paid", "accepted" };
    private static readonly List<string> OpenTicketStatuses = new() { "open", "in_progress", "waiting_customer", "reopened" };

    /// <summary>Get.</summary>
    public async Task<DashboardViewModel> GetAsync(CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return new DashboardViewModel();
        }

        var todayUtc = DateTime.UtcNow.Date;
        var startOfDay = new DateTimeOffset(todayUtc, TimeSpan.Zero);
        var endOfDay = startOfDay.AddDays(1);

        // Parallelise independent counts + lists. They all share the same DbContext, so
        // we must NOT actually run them concurrently (DbContext isn't thread-safe).
        // Sequential awaits are still one round-trip each, but cheap thanks to indexed counts.

        var activeKioskCount = await db.Kiosks.AsNoTracking()
            .CountAsync(k => k.TenantId == tenantId && k.IsActive, ct);

        var totalCustomers = await db.Customers.AsNoTracking()
            .CountAsync(c => c.TenantId == tenantId && c.DeletedAt == null, ct);

        var todaysTxQuery = db.Transactions.AsNoTracking()
            .Where(t => t.TenantId == tenantId
                && t.OccurredAt >= startOfDay
                && t.OccurredAt < endOfDay
                && PaidStatuses.Contains(t.Status));

        var todaysTxCount = await todaysTxQuery.CountAsync(ct);
        var todaysGross = await todaysTxQuery.SumAsync(t => (decimal?)t.TotalAmount, ct) ?? 0m;

        var currency = await db.Transactions.AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.OccurredAt)
            .Select(t => t.CurrencyCode)
            .FirstOrDefaultAsync(ct) ?? "INR";

        var openTickets = await db.SupportTickets.AsNoTracking()
            .CountAsync(t => t.TenantId == tenantId
                && !t.IsClosed
                && OpenTicketStatuses.Contains(t.Status), ct);

        var unresolvedExceptions = await db.ExceptionLogs.AsNoTracking()
            .CountAsync(e => (e.TenantId == tenantId || e.TenantId == null) && !e.IsResolved, ct);

        // Two-step projection: EF's shaper trips coercing DateTimeOffset →
        // Nullable<DateTime> for in-SQL conversions like .UtcDateTime. Pull the
        // raw DateTimeOffset from SQL, convert to DateTime client-side.
        var recentTxRaw = await db.Transactions.AsNoTracking()
            .Where(t => t.TenantId == tenantId && PaidStatuses.Contains(t.Status))
            .OrderByDescending(t => t.OccurredAt)
            .Take(10)
            .Select(t => new
            {
                t.TransactionCode,
                t.Kind,
                t.Status,
                t.TotalAmount,
                t.NetPayout,
                t.CurrencyCode,
                t.OccurredAt,
            })
            .ToListAsync(ct);

        var recentTx = recentTxRaw.Select(t => new RecentTransactionRow
        {
            TransactionCode = t.TransactionCode,
            Kind = t.Kind,
            Status = t.Status,
            TotalAmount = t.TotalAmount,
            NetPayout = t.NetPayout,
            CurrencyCode = t.CurrencyCode,
            OccurredOn = t.OccurredAt.UtcDateTime,
        }).ToList();

        var activeKiosksRaw = await db.Kiosks.AsNoTracking()
            .Where(k => k.TenantId == tenantId && k.IsActive)
            .OrderByDescending(k => k.LastPingAt ?? k.CreatedAt)
            .Take(10)
            .Select(k => new
            {
                k.Code,
                k.FriendlyName,
                k.Status,
                k.IsMaintenance,
                k.LastPingAt,
            })
            .ToListAsync(ct);

        var activeKiosks = activeKiosksRaw.Select(k => new ActiveKioskRow
        {
            Code = k.Code,
            FriendlyName = k.FriendlyName,
            Status = k.Status,
            IsMaintenance = k.IsMaintenance,
            LastPing = k.LastPingAt?.UtcDateTime,
        }).ToList();

        return new DashboardViewModel
        {
            ActiveKioskCount = activeKioskCount,
            TotalCustomers = totalCustomers,
            TodaysTransactionCount = todaysTxCount,
            TodaysGrossSales = todaysGross,
            CurrencyCode = currency,
            OpenTickets = openTickets,
            UnresolvedExceptions = unresolvedExceptions,
            RecentTransactions = recentTx,
            ActiveKiosks = activeKiosks,
        };
    }
}
