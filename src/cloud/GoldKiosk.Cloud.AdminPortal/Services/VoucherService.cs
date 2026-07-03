using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using RedemptionPolicyEntity = GoldKiosk.Infrastructure.Entities.Voucher.RedemptionPolicy;
using VoucherEntity = GoldKiosk.Infrastructure.Entities.Voucher.Voucher;

namespace GoldKiosk.Cloud.AdminPortal.Services;
/// <summary>Voucher service.</summary>
public sealed class VoucherService(AppDbContext db, ICurrentUserService currentUser) : IVoucherService
{
    // ─── Vouchers ──────────────────────────────────────────────────────────

    /// <summary>List vouchers.</summary>
    public async Task<VoucherList> ListVouchersAsync(string? search, string? status, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return new VoucherList { PageSize = pageSize, PageNo = pageNo };
        }

        var baseQ = db.Vouchers.AsNoTracking().Where(v => v.TenantId == tenantId);
        var q = baseQ;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(v => EF.Functions.ILike(v.Code, s) || EF.Functions.ILike(v.Name, s));
        }

        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(v => v.IsActive);
        }
        else if (string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            q = q.Where(v => !v.IsActive);
        }

        var totals = await baseQ
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(v => v.IsActive),
                InActive = g.Count(v => !v.IsActive)
            })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await q.CountAsync(ct);
        // Two-step projection: EF's shaper can't coerce DateTimeOffset → DateTime in SQL.
        var rawItems = await q
            .OrderByDescending(v => v.CreatedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(v => new
            {
                v.Id,
                v.Code,
                v.Name,
                v.Description,
                v.DiscountType,
                v.DiscountValue,
                v.CurrencyCode,
                v.ValidityStart,
                v.ValidityEnd,
                v.UsageLimit,
                v.UsedCount,
                v.IsActive,
                v.CreatedAt,
            })
            .ToListAsync(ct);

        var items = rawItems.Select(v => new VoucherViewModel
        {
            Id = v.Id,
            Code = v.Code,
            Name = v.Name,
            Description = v.Description,
            DiscountType = v.DiscountType,
            DiscountValue = v.DiscountValue,
            CurrencyCode = v.CurrencyCode,
            ValidityStart = v.ValidityStart.UtcDateTime,
            ValidityEnd = v.ValidityEnd.UtcDateTime,
            UsageLimit = v.UsageLimit,
            UsedCount = v.UsedCount,
            IsActive = v.IsActive,
            CreatedOn = v.CreatedAt.UtcDateTime,
        }).ToList();

        return new VoucherList
        {
            Items = items,
            Total = totals?.Total ?? 0,
            Active = totals?.Active ?? 0,
            InActive = totals?.InActive ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    /// <summary>Get voucher.</summary>
    public async Task<VoucherViewModel?> GetVoucherAsync(Guid id, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return null;
        }

        var v = await db.Vouchers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        return v is null ? null : Map(v);
    }

    /// <summary>Add voucher.</summary>
    public async Task<OperationResult> AddVoucherAsync(VoucherViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.Code) || string.IsNullOrWhiteSpace(vm.Name))
        {
            return OperationResult.Fail("Voucher code and name are required.");
        }

        if (!IsValidDiscountType(vm.DiscountType))
        {
            return OperationResult.Fail("Discount type must be 'percent' or 'fixed'.");
        }

        if (vm.ValidityEnd < vm.ValidityStart)
        {
            return OperationResult.Fail("Validity end must be on or after validity start.");
        }

        var duplicate = await db.Vouchers.AnyAsync(v => v.TenantId == tenantId && v.Code == vm.Code, ct);
        if (duplicate)
        {
            return OperationResult.Fail($"Voucher with code '{vm.Code}' already exists.");
        }

        var v = new VoucherEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = vm.Code!,
            Name = vm.Name!,
            Description = vm.Description,
            DiscountType = vm.DiscountType!,
            DiscountValue = vm.DiscountValue,
            CurrencyCode = string.IsNullOrWhiteSpace(vm.CurrencyCode) ? null : vm.CurrencyCode,
            ValidityStart = DateTime.SpecifyKind(vm.ValidityStart, DateTimeKind.Utc),
            ValidityEnd = DateTime.SpecifyKind(vm.ValidityEnd, DateTimeKind.Utc),
            UsageLimit = vm.UsageLimit,
            IsActive = vm.IsActive,
        };

        db.Vouchers.Add(v);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit voucher.</summary>
    public async Task<OperationResult> EditVoucherAsync(VoucherViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (vm.Id == Guid.Empty)
        {
            return OperationResult.Fail("Voucher id is required.");
        }

        if (!IsValidDiscountType(vm.DiscountType))
        {
            return OperationResult.Fail("Discount type must be 'percent' or 'fixed'.");
        }

        if (vm.ValidityEnd < vm.ValidityStart)
        {
            return OperationResult.Fail("Validity end must be on or after validity start.");
        }

        var v = await db.Vouchers.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == vm.Id, ct);
        if (v is null)
        {
            return OperationResult.Fail("Voucher not found.");
        }

        if (!string.IsNullOrWhiteSpace(vm.Name))
        {
            v.Name = vm.Name!;
        }

        if (!string.IsNullOrWhiteSpace(vm.Description))
        {
            v.Description = vm.Description;
        }

        if (!string.IsNullOrWhiteSpace(vm.DiscountType))
        {
            v.DiscountType = vm.DiscountType!;
        }

        v.DiscountValue = vm.DiscountValue;
        v.CurrencyCode = string.IsNullOrWhiteSpace(vm.CurrencyCode) ? null : vm.CurrencyCode;
        v.ValidityStart = DateTime.SpecifyKind(vm.ValidityStart, DateTimeKind.Utc);
        v.ValidityEnd = DateTime.SpecifyKind(vm.ValidityEnd, DateTimeKind.Utc);
        v.UsageLimit = vm.UsageLimit;
        v.IsActive = vm.IsActive;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Delete voucher.</summary>
    public async Task<OperationResult> DeleteVoucherAsync(Guid id, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var v = await db.Vouchers.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        if (v is null)
        {
            return OperationResult.Fail("Voucher not found.");
        }

        // Soft-disable rather than hard-delete: redemption rows reference us.
        v.IsActive = false;
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    // ─── Redemption policies ───────────────────────────────────────────────

    /// <summary>List policies.</summary>
    public async Task<RedemptionPolicyList> ListPoliciesAsync(string? search, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return new RedemptionPolicyList { PageSize = pageSize, PageNo = pageNo };
        }

        var baseQ =
            from p in db.RedemptionPolicies.AsNoTracking()
            join v in db.Vouchers.AsNoTracking() on p.VoucherId equals v.Id
            where v.TenantId == tenantId
            select new { p, v };

        var q = baseQ;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.v.Code, s) || EF.Functions.ILike(x.v.Name, s));
        }

        var now = DateTimeOffset.UtcNow;
        var totals = await baseQ
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(x => x.p.EffectiveTo == null || x.p.EffectiveTo > now),
                Expired = g.Count(x => x.p.EffectiveTo != null && x.p.EffectiveTo <= now),
            })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await q.CountAsync(ct);
        // Two-step projection: EF shaper can't coerce DateTimeOffset → Nullable<DateTime>.
        var rawPolicies = await q
            .OrderByDescending(x => x.p.EffectiveFrom)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(x => new
            {
                PolicyId = x.p.Id,
                VoucherId = x.v.Id,
                VoucherCode = x.v.Code,
                VoucherName = x.v.Name,
                x.p.MinPurchaseAmount,
                x.p.MaxDiscountCap,
                x.p.PerCustomerLimit,
                x.p.ApplicableCategories,
                x.p.EffectiveFrom,
                x.p.EffectiveTo,
            })
            .ToListAsync(ct);

        var items = rawPolicies.Select(x => new RedemptionPolicyViewModel
        {
            Id = x.PolicyId,
            VoucherId = x.VoucherId,
            VoucherCode = x.VoucherCode,
            VoucherName = x.VoucherName,
            MinPurchaseAmount = x.MinPurchaseAmount,
            MaxDiscountCap = x.MaxDiscountCap,
            PerCustomerLimit = x.PerCustomerLimit,
            ApplicableCategories = x.ApplicableCategories,
            EffectiveFrom = x.EffectiveFrom.UtcDateTime,
            EffectiveTo = x.EffectiveTo?.UtcDateTime,
        }).ToList();

        return new RedemptionPolicyList
        {
            Items = items,
            Total = totals?.Total ?? 0,
            Active = totals?.Active ?? 0,
            Expired = totals?.Expired ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    /// <summary>Add policy.</summary>
    public async Task<OperationResult> AddPolicyAsync(RedemptionPolicyViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (vm.VoucherId == Guid.Empty)
        {
            return OperationResult.Fail("Voucher is required.");
        }

        var ownsVoucher = await db.Vouchers
            .AnyAsync(v => v.TenantId == tenantId && v.Id == vm.VoucherId, ct);
        if (!ownsVoucher)
        {
            return OperationResult.Fail("Voucher not found.");
        }

        var p = new RedemptionPolicyEntity
        {
            Id = Guid.NewGuid(),
            VoucherId = vm.VoucherId,
            MinPurchaseAmount = vm.MinPurchaseAmount,
            MaxDiscountCap = vm.MaxDiscountCap,
            PerCustomerLimit = vm.PerCustomerLimit,
            ApplicableCategories = string.IsNullOrWhiteSpace(vm.ApplicableCategories) ? null : vm.ApplicableCategories,
            EffectiveFrom = DateTime.SpecifyKind(vm.EffectiveFrom, DateTimeKind.Utc),
            EffectiveTo = vm.EffectiveTo is null ? null : DateTime.SpecifyKind(vm.EffectiveTo.Value, DateTimeKind.Utc),
        };

        db.RedemptionPolicies.Add(p);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit policy.</summary>
    public async Task<OperationResult> EditPolicyAsync(RedemptionPolicyViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (vm.Id == Guid.Empty)
        {
            return OperationResult.Fail("Policy id is required.");
        }

        var p = await (
            from x in db.RedemptionPolicies
            join v in db.Vouchers on x.VoucherId equals v.Id
            where x.Id == vm.Id && v.TenantId == tenantId
            select x).FirstOrDefaultAsync(ct);
        if (p is null)
        {
            return OperationResult.Fail("Policy not found.");
        }

        p.MinPurchaseAmount = vm.MinPurchaseAmount;
        p.MaxDiscountCap = vm.MaxDiscountCap;
        p.PerCustomerLimit = vm.PerCustomerLimit;
        p.ApplicableCategories = string.IsNullOrWhiteSpace(vm.ApplicableCategories) ? null : vm.ApplicableCategories;
        p.EffectiveFrom = DateTime.SpecifyKind(vm.EffectiveFrom, DateTimeKind.Utc);
        p.EffectiveTo = vm.EffectiveTo is null ? null : DateTime.SpecifyKind(vm.EffectiveTo.Value, DateTimeKind.Utc);

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Delete policy.</summary>
    public async Task<OperationResult> DeletePolicyAsync(Guid id, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var p = await (
            from x in db.RedemptionPolicies
            join v in db.Vouchers on x.VoucherId equals v.Id
            where x.Id == id && v.TenantId == tenantId
            select x).FirstOrDefaultAsync(ct);
        if (p is null)
        {
            return OperationResult.Fail("Policy not found.");
        }

        db.RedemptionPolicies.Remove(p);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    // ─── Voucher redemptions (read-only) ────────────────────────────────────

    /// <summary>List redemptions.</summary>
    public async Task<VoucherRedemptionList> ListRedemptionsAsync(string? search, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return new VoucherRedemptionList { PageSize = pageSize, PageNo = pageNo };
        }

        var baseQ =
            from r in db.VoucherRedemptions.AsNoTracking()
            join v in db.Vouchers.AsNoTracking() on r.VoucherId equals v.Id
            where v.TenantId == tenantId
            select new { r, v };

        var q = baseQ;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.v.Code, s) || EF.Functions.ILike(x.v.Name, s));
        }

        var totalFiltered = await q.CountAsync(ct);
        // Two-step projection: EF shaper can't coerce DateTimeOffset → DateTime in SQL.
        var rawRedemptions = await q
            .OrderByDescending(x => x.r.RedeemedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(x => new
            {
                RedemptionId = x.r.Id,
                VoucherCode = x.v.Code,
                VoucherName = x.v.Name,
                x.r.CustomerId,
                x.r.TransactionId,
                x.r.KioskId,
                x.r.RedeemedAmount,
                x.r.RedeemedAt,
            })
            .ToListAsync(ct);

        var items = rawRedemptions.Select(x => new VoucherRedemptionViewModel
        {
            Id = x.RedemptionId,
            VoucherCode = x.VoucherCode,
            VoucherName = x.VoucherName,
            CustomerId = x.CustomerId,
            TransactionId = x.TransactionId,
            KioskId = x.KioskId,
            RedeemedAmount = x.RedeemedAmount,
            RedeemedOn = x.RedeemedAt.UtcDateTime,
        }).ToList();

        return new VoucherRedemptionList
        {
            Items = items,
            Total = totalFiltered,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    // ─── helpers ───────────────────────────────────────────────────────────

    private static bool IsValidDiscountType(string? s)
        => !string.IsNullOrWhiteSpace(s) && (s is "percent" or "fixed");

    private static VoucherViewModel Map(VoucherEntity v) => new()
    {
        Id = v.Id,
        Code = v.Code,
        Name = v.Name,
        Description = v.Description,
        DiscountType = v.DiscountType,
        DiscountValue = v.DiscountValue,
        CurrencyCode = v.CurrencyCode,
        ValidityStart = v.ValidityStart.UtcDateTime,
        ValidityEnd = v.ValidityEnd.UtcDateTime,
        UsageLimit = v.UsageLimit,
        UsedCount = v.UsedCount,
        IsActive = v.IsActive,
        CreatedOn = v.CreatedAt.UtcDateTime,
    };
}
