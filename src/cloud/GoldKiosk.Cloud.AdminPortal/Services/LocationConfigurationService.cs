using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Kiosk;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Location configuration service.</summary>
public sealed class LocationConfigurationService(AppDbContext db, ICurrentUserService currentUser) : ILocationConfigurationService
{
    /// <summary>List.</summary>
    public async Task<KioskLocationList> ListAsync(string? search, string? stat, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return EmptyResult(pageSize, pageNo);
        }

        var q = db.KioskLocations.AsNoTracking().Where(l => l.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(l => EF.Functions.ILike(l.Code, s) || EF.Functions.ILike(l.Name, s) || EF.Functions.ILike(l.City, s));
        }

        if (bool.TryParse(stat, out var isActive))
        {
            q = q.Where(l => l.IsActive == isActive);
        }

        var totals = await db.KioskLocations.AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Active = g.Count(l => l.IsActive), InActive = g.Count(l => !l.IsActive) })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await q.CountAsync(ct);

        var items = await q
            .OrderBy(l => l.Code)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(l => new KioskLocationViewModel
            {
                Id = l.Id,
                StoreId = l.StoreId,
                Code = l.Code,
                Name = l.Name,
                City = l.City,
                Address = l.Address,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                Region = l.Region,
                IsActive = l.IsActive,
            })
            .ToListAsync(ct);

        return new KioskLocationList
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

    /// <summary>Add.</summary>
    public async Task<OperationResult> AddAsync(KioskLocationViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.Code))
        {
            return OperationResult.Fail("Code is required.");
        }

        if (string.IsNullOrWhiteSpace(vm.Name))
        {
            return OperationResult.Fail("Name is required.");
        }

        if (string.IsNullOrWhiteSpace(vm.City))
        {
            return OperationResult.Fail("City is required.");
        }

        var duplicate = await db.KioskLocations.AnyAsync(l => l.TenantId == tenantId && l.Code == vm.Code, ct);
        if (duplicate)
        {
            return OperationResult.Fail($"Location '{vm.Code}' already exists.");
        }

        var l = new KioskLocation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            StoreId = vm.StoreId,
            Code = vm.Code,
            Name = vm.Name,
            City = vm.City,
            Address = vm.Address,
            Latitude = vm.Latitude,
            Longitude = vm.Longitude,
            Region = vm.Region,
            IsActive = vm.IsActive,
        };
        db.KioskLocations.Add(l);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit.</summary>
    public async Task<OperationResult> EditAsync(KioskLocationViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var l = await db.KioskLocations.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == vm.Id, ct);
        if (l is null)
        {
            return OperationResult.Fail("Location not found.");
        }

        if (!string.IsNullOrWhiteSpace(vm.Name))
        {
            l.Name = vm.Name;
        }

        if (!string.IsNullOrWhiteSpace(vm.City))
        {
            l.City = vm.City;
        }

        l.Address = vm.Address;
        l.Latitude = vm.Latitude;
        l.Longitude = vm.Longitude;
        l.Region = vm.Region;
        l.StoreId = vm.StoreId;
        l.IsActive = vm.IsActive;

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

        var l = await db.KioskLocations.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        if (l is null)
        {
            return OperationResult.Fail("Location not found.");
        }

        // Soft-delete (kiosks may still FK to this row).
        l.IsActive = false;
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    private static KioskLocationList EmptyResult(int pageSize, int pageNo) => new()
    {
        Items = new List<KioskLocationViewModel>(),
        PageSize = pageSize,
        PageNo = pageNo,
    };
}
