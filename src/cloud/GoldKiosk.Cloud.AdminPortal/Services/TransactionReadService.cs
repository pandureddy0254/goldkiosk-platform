using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>
/// Read-side service that powers the AdminDashboard Transaction History view.
/// Reversal is a stub for this pass — the real compensating-ledger + payout-refund
/// pipeline lands in a later module. The DB marker (<c>tx.transactions.reversed_at</c>)
/// is set here so the UI can show "Is Reversed".
/// </summary>
public sealed class TransactionReadService(AppDbContext db, ICurrentUserService currentUser) : ITransactionReadService
{
    /// <summary>List for customer.</summary>
    public async Task<TransactionHistoryList> ListForCustomerAsync(
        string customerCode, string? search, string? status, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return Empty(pageSize, pageNo);
        }

        var customer = await db.Customers.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.CustomerCode == customerCode)
            .Select(c => new { c.Id, c.CustomerCode })
            .FirstOrDefaultAsync(ct);
        if (customer is null)
        {
            return Empty(pageSize, pageNo);
        }

        var q = db.Transactions.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.CustomerId == customer.Id);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(t => EF.Functions.ILike(t.TransactionCode, s));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            q = q.Where(t => t.Status == status);
        }

        var totals = await db.Transactions.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.CustomerId == customer.Id)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Success = g.Count(t => t.Status == "paid"),
                Reversed = g.Count(t => t.ReversedAt != null),
                Failure = g.Count(t => t.Status == "declined" || t.Status == "aborted"),
            })
            .FirstOrDefaultAsync(ct);

        var pageCount = await q.CountAsync(ct);

        // Two-step projection: keep DateTimeOffset native in SQL, convert client-side.
        var raw = await q
            .OrderByDescending(t => t.OccurredAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.TransactionCode,
                t.TotalAmount,
                t.Fees,
                t.Status,
                t.ReversedAt,
                t.CurrencyCode,
                t.OccurredAt,
            })
            .ToListAsync(ct);

        var page = raw.Select(t => new TransactionHistoryViewModel
        {
            Sr = t.TransactionCode,
            MobileMasked = "***",
            TransactionId = t.Id,
            TransactionCode = t.TransactionCode,
            Amount = t.TotalAmount,
            Commission = 0m,
            ServiceFee = t.Fees,
            OriginalStatus = t.Status,
            FinalStatus = t.ReversedAt != null ? "reversed" : t.Status,
            IsReversed = t.ReversedAt != null,
            CurrencyCode = t.CurrencyCode,
            OccurredOn = t.OccurredAt.UtcDateTime,
        }).ToList();

        return new TransactionHistoryList
        {
            transactionList = page,
            Total = totals?.Total ?? 0,
            Success = totals?.Success ?? 0,
            Reversed = totals?.Reversed ?? 0,
            Failure = totals?.Failure ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = pageCount,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(pageCount / (double)pageSize) : 0,
            CustomerId = customer.Id,
            CustomerCode = customer.CustomerCode,
        };
    }

    /// <summary>Reverse.</summary>
    public async Task<OperationResult> ReverseAsync(Guid transactionId, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var tx = await db.Transactions
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.TenantId == tenantId, ct);
        if (tx is null)
        {
            return OperationResult.Fail("Transaction not found.");
        }

        if (tx.ReversedAt is not null)
        {
            return OperationResult.Fail("Transaction is already reversed.");
        }

        // Stub: mark the transaction as reversed. The real flow (compensating ledger
        // entry via customer.post_ledger_entry, payout refund, audit trail row) will
        // be implemented when the reversal module lands.
        tx.ReversedAt = DateTimeOffset.UtcNow;
        tx.ReversedByUserId = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    private static TransactionHistoryList Empty(int pageSize, int pageNo) => new()
    {
        transactionList = new List<TransactionHistoryViewModel>(),
        Total = 0,
        Success = 0,
        Reversed = 0,
        Failure = 0,
        PageSize = pageSize,
        PageNo = pageNo,
        PageCount = 0,
        TotalPage = 0,
    };
}
