using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Franchise wallet service.</summary>
public sealed class FranchiseWalletService(AppDbContext db, ICurrentUserService currentUser) : IFranchiseWalletService
{
    /// <summary>List.</summary>
    public async Task<FranchiseWalletsList> ListAsync(string? search, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return new FranchiseWalletsList { PageSize = pageSize, PageNo = pageNo };
        }

        // Wallets belong to merchants of this tenant; join through Merchants for the filter.
        var walletsQ =
            from w in db.FranchiseWallets.AsNoTracking()
            join m in db.Merchants.AsNoTracking() on w.MerchantId equals m.Id
            where m.TenantId == tenantId && m.DeletedAt == null
            select new { w, m };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            walletsQ = walletsQ.Where(x => EF.Functions.ILike(x.m.Code, s) || EF.Functions.ILike(x.m.Name, s));
        }

        // Two-step projection: keep DateTimeOffset native in SQL, convert client-side.
        var walletsRaw = await walletsQ
            .OrderByDescending(x => x.w.CreatedAt)
            .Select(x => new
            {
                WalletId = x.w.Id,
                MerchantId = x.m.Id,
                MerchantCode = x.m.Code,
                MerchantName = x.m.Name,
                x.w.Balance,
                x.w.CurrencyCode,
                x.w.LastTopupAt,
            })
            .ToListAsync(ct);

        var wallets = walletsRaw.Select(x => new FranchiseWalletViewModel
        {
            WalletId = x.WalletId,
            MerchantId = x.MerchantId,
            MerchantCode = x.MerchantCode,
            MerchantName = x.MerchantName,
            Balance = x.Balance,
            CurrencyCode = x.CurrencyCode,
            LastTopupOn = x.LastTopupAt?.UtcDateTime,
        }).ToList();

        // Topup requests (joined back to merchant for display) - two-step projection.
        var requestsRaw = await (
            from t in db.WalletTopupRequests.AsNoTracking()
            join w in db.FranchiseWallets.AsNoTracking() on t.WalletId equals w.Id
            join m in db.Merchants.AsNoTracking() on w.MerchantId equals m.Id
            where m.TenantId == tenantId && m.DeletedAt == null
            orderby t.RequestedAt descending
            select new
            {
                t.Id,
                t.WalletId,
                MerchantCode = m.Code,
                MerchantName = m.Name,
                t.RequestedAmount,
                t.CurrencyCode,
                t.Status,
                t.PaymentReference,
                t.RejectedReason,
                t.RequestedAt,
                t.ApprovedAt,
            }).ToListAsync(ct);

        var all = requestsRaw.Select(t => new WalletTopupRequestViewModel
        {
            Id = t.Id,
            WalletId = t.WalletId,
            MerchantCode = t.MerchantCode,
            MerchantName = t.MerchantName,
            RequestedAmount = t.RequestedAmount,
            CurrencyCode = t.CurrencyCode,
            Status = t.Status,
            PaymentReference = t.PaymentReference,
            RejectedReason = t.RejectedReason,
            RequestedOn = t.RequestedAt.UtcDateTime,
            ApprovedOn = t.ApprovedAt?.UtcDateTime,
        }).ToList();

        return new FranchiseWalletsList
        {
            Wallets = wallets,
            All = all,
            Approved = all.Where(x => x.Status == "approved").ToList(),
            Pending = all.Where(x => x.Status == "pending").ToList(),
            Rejected = all.Where(x => x.Status == "rejected").ToList(),
            PageSize = pageSize,
            PageNo = pageNo,
        };
    }

    /// <summary>Approve topup.</summary>
    public async Task<OperationResult> ApproveTopupAsync(Guid topupId, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        // Lookup the topup and its wallet+merchant, scoped to this tenant.
        var row = await (
            from t in db.WalletTopupRequests
            join w in db.FranchiseWallets on t.WalletId equals w.Id
            join m in db.Merchants on w.MerchantId equals m.Id
            where t.Id == topupId && m.TenantId == tenantId
            select new { t, w }).FirstOrDefaultAsync(ct);

        if (row is null)
        {
            return OperationResult.Fail("Topup request not found.");
        }

        if (row.t.Status != "pending")
        {
            return OperationResult.Fail($"Topup is already '{row.t.Status}'.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Atomic ledger posting + balance update via PL/pgSQL function.
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                SELECT merchant.post_franchise_ledger_entry(
                    {row.w.Id}::uuid,
                    'topup'::text,
                    {row.t.RequestedAmount}::public.domain_money,
                    'wallet_topup_request'::text,
                    {row.t.Id}::uuid,
                    {currentUser.UserId}::uuid)", ct);

            row.t.Status = "approved";
            row.t.ApprovedAt = DateTimeOffset.UtcNow;
            row.t.ApprovedByUserId = currentUser.UserId;
            row.t.RejectedReason = null;

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return OperationResult.Ok();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            return OperationResult.Fail($"Approval failed: {ex.Message}");
        }
    }

    /// <summary>Reject topup.</summary>
    public async Task<OperationResult> RejectTopupAsync(Guid topupId, string? reason, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var t = await (
            from r in db.WalletTopupRequests
            join w in db.FranchiseWallets on r.WalletId equals w.Id
            join m in db.Merchants on w.MerchantId equals m.Id
            where r.Id == topupId && m.TenantId == tenantId
            select r).FirstOrDefaultAsync(ct);

        if (t is null)
        {
            return OperationResult.Fail("Topup request not found.");
        }

        if (t.Status != "pending")
        {
            return OperationResult.Fail($"Topup is already '{t.Status}'.");
        }

        t.Status = "rejected";
        t.RejectedReason = reason;
        t.ApprovedAt = DateTimeOffset.UtcNow;
        t.ApprovedByUserId = currentUser.UserId;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }
}
