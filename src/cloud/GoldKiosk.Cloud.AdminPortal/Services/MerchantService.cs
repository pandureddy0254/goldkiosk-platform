using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using MerchantEntity = GoldKiosk.Infrastructure.Entities.Merchant.Merchant;

namespace GoldKiosk.Cloud.AdminPortal.Services;
/// <summary>Merchant service.</summary>
public sealed class MerchantService(AppDbContext db, ICurrentUserService currentUser) : IMerchantService
{
    /// <summary>List.</summary>
    public async Task<MerchantList> ListAsync(string? search, string? kycStatus, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return Empty(pageSize, pageNo);
        }

        var baseQ = db.Merchants.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.DeletedAt == null);

        var q = baseQ;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(m => EF.Functions.ILike(m.Code, s) || EF.Functions.ILike(m.Name, s));
        }

        if (!string.IsNullOrWhiteSpace(kycStatus))
        {
            q = q.Where(m => m.KycStatus == kycStatus);
        }

        var totals = await baseQ
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Approved = g.Count(m => m.KycStatus == "approved"),
                Pending = g.Count(m => m.KycStatus == "pending"),
                Rejected = g.Count(m => m.KycStatus == "rejected"),
            })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await q.CountAsync(ct);

        // Two-step projection: keep DateTimeOffset native in SQL, convert client-side.
        var raw = await q
            .OrderByDescending(m => m.CreatedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.Code,
                m.Name,
                m.KycStatus,
                m.IsActive,
                m.CreatedAt,
            })
            .ToListAsync(ct);

        var items = raw.Select(m => new MerchantViewModel
        {
            Id = m.Id,
            Code = m.Code,
            Name = m.Name,
            Email = string.Empty,         // never expose *_enc in lists
            Mobile = string.Empty,
            Address = string.Empty,
            KycStatus = m.KycStatus,
            IsActive = m.IsActive,
            CreatedOn = m.CreatedAt.UtcDateTime,
        }).ToList();

        return new MerchantList
        {
            Items = items,
            Total = totals?.Total ?? 0,
            Approved = totals?.Approved ?? 0,
            Pending = totals?.Pending ?? 0,
            Rejected = totals?.Rejected ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    /// <summary>Get by id.</summary>
    public async Task<MerchantViewModel?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return null;
        }

        var m = await db.Merchants.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && x.DeletedAt == null, ct);
        return m is null ? null : new MerchantViewModel
        {
            Id = m.Id,
            Code = m.Code,
            Name = m.Name,
            KycStatus = m.KycStatus,
            IsActive = m.IsActive,
            CreatedOn = m.CreatedAt.UtcDateTime,
        };
    }

    /// <summary>Add.</summary>
    public async Task<OperationResult> AddAsync(MerchantViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.Code) || string.IsNullOrWhiteSpace(vm.Name))
        {
            return OperationResult.Fail("Merchant code and name are required.");
        }

        if (!IsValidKycStatus(vm.KycStatus))
        {
            return OperationResult.Fail("Invalid KYC status.");
        }

        var duplicate = await db.Merchants.AnyAsync(m => m.TenantId == tenantId && m.Code == vm.Code, ct);
        if (duplicate)
        {
            return OperationResult.Fail($"Merchant with code '{vm.Code}' already exists.");
        }

        var m = new MerchantEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = vm.Code!,
            Name = vm.Name!,
            KycStatus = string.IsNullOrWhiteSpace(vm.KycStatus) ? "pending" : vm.KycStatus!,
            IsActive = vm.IsActive,
            // *_enc columns left null — proper pgcrypto encryption comes in a later pass.
        };

        db.Merchants.Add(m);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit.</summary>
    public async Task<OperationResult> EditAsync(MerchantViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (vm.Id == Guid.Empty)
        {
            return OperationResult.Fail("Merchant id is required.");
        }

        if (!IsValidKycStatus(vm.KycStatus))
        {
            return OperationResult.Fail("Invalid KYC status.");
        }

        var m = await db.Merchants.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == vm.Id && x.DeletedAt == null, ct);
        if (m is null)
        {
            return OperationResult.Fail("Merchant not found.");
        }

        if (!string.IsNullOrWhiteSpace(vm.Name))
        {
            m.Name = vm.Name!;
        }

        if (!string.IsNullOrWhiteSpace(vm.KycStatus))
        {
            m.KycStatus = vm.KycStatus!;
        }

        m.IsActive = vm.IsActive;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Delete.</summary>
    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var m = await db.Merchants.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.Id == id && x.DeletedAt == null, ct);
        if (m is null)
        {
            return OperationResult.Fail("Merchant not found.");
        }

        m.DeletedAt = DateTimeOffset.UtcNow;
        m.IsActive = false;
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    private static bool IsValidKycStatus(string? s)
        => string.IsNullOrWhiteSpace(s) || s is "pending" or "approved" or "rejected" or "review";

    private static MerchantList Empty(int pageSize, int pageNo) => new()
    {
        Items = new(),
        PageSize = pageSize,
        PageNo = pageNo,
    };
}
