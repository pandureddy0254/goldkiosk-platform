using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using KioskEntity = GoldKiosk.Infrastructure.Entities.Kiosk.Kiosk;

namespace GoldKiosk.Cloud.AdminPortal.Services;
/// <summary>Kiosk service.</summary>
public sealed class KioskService(AppDbContext db, ICurrentUserService currentUser, IPasswordHasher<AppUser> passwordHasher) : IKioskService
{
    /// <summary>List.</summary>
    public async Task<KioskMasterList> ListAsync(string? search, string? status, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return EmptyResult(pageSize, pageNo);
        }

        var q = db.Kiosks.AsNoTracking().Where(k => k.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(k => EF.Functions.ILike(k.Code, s) || EF.Functions.ILike(k.FriendlyName, s));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            q = q.Where(k => k.Status == status);
        }

        var totals = await db.Kiosks.AsNoTracking()
            .Where(k => k.TenantId == tenantId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(k => k.IsActive),
                InActive = g.Count(k => !k.IsActive)
            })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(k => k.CreatedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(k => Map(k))
            .ToListAsync(ct);

        return new KioskMasterList
        {
            kioskMasterList = items,
            Total = totals?.Total ?? 0,
            Active = totals?.Active ?? 0,
            InActive = totals?.InActive ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    /// <summary>Get by code.</summary>
    public async Task<KioskMasterViewModel?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return null;
        }

        var k = await db.Kiosks.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        return k is null ? null : Map(k);
    }

    /// <summary>Add.</summary>
    public async Task<OperationResult> AddAsync(KioskMasterViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.KioskCode) || string.IsNullOrWhiteSpace(vm.KioskName))
        {
            return OperationResult.Fail("Kiosk code and name are required.");
        }

        var duplicate = await db.Kiosks.AnyAsync(k => k.TenantId == tenantId && k.Code == vm.KioskCode, ct);
        if (duplicate)
        {
            return OperationResult.Fail($"Kiosk with code '{vm.KioskCode}' already exists.");
        }

        // Store id is required at the DB level. Pick the first store, or auto-create a default one.
        var defaultStore = await db.Database
            .SqlQueryRaw<Guid>("SELECT id AS \"Value\" FROM store.stores WHERE tenant_id = {0} AND deleted_at IS NULL ORDER BY created_at LIMIT 1", tenantId)
            .FirstOrDefaultAsync(ct);

        if (defaultStore == default)
        {
            defaultStore = Guid.NewGuid();
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO store.stores (id, tenant_id, code, display_name, status, time_zone, created_at, updated_at) " +
                "VALUES ({0}, {1}, 'DEFAULT', 'Default Store', 'active', 'UTC', now(), now())",
                defaultStore, tenantId);
        }

        var k = new KioskEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StoreId = defaultStore,
            Code = vm.KioskCode!,
            FriendlyName = vm.KioskName!,
            HardwareModel = "GK-CUBE-V3",
            MsixChannel = "ring1",
            Status = "onboarding",
            IsActive = true,
            IsMaintenance = false,
            OnboardedAt = vm.OnboardingDate == default
                ? DateTimeOffset.UtcNow
                : new DateTimeOffset(DateTime.SpecifyKind(vm.OnboardingDate, DateTimeKind.Utc), TimeSpan.Zero),
            PinHash = string.IsNullOrWhiteSpace(vm.KioskPin) ? null : passwordHasher.HashPassword(new AppUser(), vm.KioskPin!),
            DeviceId = vm.DeviceId,
        };

        db.Kiosks.Add(k);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit.</summary>
    public async Task<OperationResult> EditAsync(KioskMasterViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.KioskCode))
        {
            return OperationResult.Fail("Kiosk code is required.");
        }

        var k = await db.Kiosks.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == vm.KioskCode, ct);
        if (k is null)
        {
            return OperationResult.Fail($"Kiosk '{vm.KioskCode}' not found.");
        }

        if (!string.IsNullOrWhiteSpace(vm.KioskName))
        {
            k.FriendlyName = vm.KioskName!;
        }

        if (!string.IsNullOrWhiteSpace(vm.KioskStatus))
        {
            k.Status = vm.KioskStatus!;
        }

        k.IsActive = vm.IsActive;
        k.IsMaintenance = vm.IsMaintenance;
        if (!string.IsNullOrWhiteSpace(vm.KioskPin))
        {
            k.PinHash = passwordHasher.HashPassword(new AppUser(), vm.KioskPin!);
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Delete.</summary>
    public async Task<OperationResult> DeleteAsync(string code, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var k = await db.Kiosks.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);
        if (k is null)
        {
            return OperationResult.Fail($"Kiosk '{code}' not found.");
        }

        // Soft-delete via DecommissionedAt + IsActive=false (no hard delete on a kiosk).
        k.IsActive = false;
        k.Status = "decommissioned";
        k.DecommissionedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    // ─── helpers ───────────────────────────────────────────────────────────────
    private static KioskMasterViewModel Map(KioskEntity k) => new()
    {
        KioskCode = k.Code,
        KioskName = k.FriendlyName,
        ClientCode = k.TenantId.ToString(),
        KioskPin = string.Empty,                           // never expose pin
        KioskStatus = k.Status,
        Address = string.Empty,                            // joined via location in a later pass
        DeviceId = k.DeviceId ?? string.Empty,
        NetworkSpeed = k.NetworkSpeed ?? string.Empty,
        IsMaintenance = k.IsMaintenance,
        IsActive = k.IsActive,
        LastPing = k.LastPingAt?.UtcDateTime ?? default,
        CreatedOn = k.CreatedAt.UtcDateTime,
        UpdatedOn = k.UpdatedAt.UtcDateTime,
        OnboardingDate = k.OnboardedAt?.UtcDateTime ?? default,
    };

    private static KioskMasterList EmptyResult(int pageSize, int pageNo) => new()
    {
        kioskMasterList = new List<KioskMasterViewModel>(),
        Total = 0,
        Active = 0,
        InActive = 0,
        PageSize = pageSize,
        PageNo = pageNo,
        PageCount = 0,
        TotalPage = 0,
    };
}
