using System.Globalization;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Kiosk;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Screen saver service.</summary>
public sealed class ScreenSaverService(AppDbContext db, ICurrentUserService currentUser) : IScreenSaverService
{
    /// <summary>List.</summary>
    public async Task<ScreenSaverMasterList> ListAsync(string? search, string? status, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return new ScreenSaverMasterList
            {
                screenSaverMasterList = new List<ScreenSaverMasterViewModel>(),
                PageSize = pageSize,
                PageNo = pageNo,
            };
        }

        var q = db.ScreenSavers.AsNoTracking().Where(s => s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var p = $"%{search.Trim()}%";
            q = q.Where(s => EF.Functions.ILike(s.Code, p));
        }
        if (!string.IsNullOrWhiteSpace(status) && bool.TryParse(status, out var b))
        {
            q = q.Where(s => s.IsActive == b);
        }

        var totals = await db.ScreenSavers.AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(s => s.IsActive),
                InActive = g.Count(s => !s.IsActive),
            })
            .FirstOrDefaultAsync(ct);

        var filtered = await q.CountAsync(ct);

        // Pull the raw rows first, then project to the VM in memory so that
        // CLR-only calls like `.UtcDateTime` don't leak into the SQL translator
        // (EF Core 10 + Npgsql 9 trip on this with a 'No coercion operator…'
        // InvalidOperationException).
        var raw = await q.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Code)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(s => new { s.Code, s.ImageUrl, s.TenantId, s.DisplayOrder, s.IsActive, s.CreatedAt })
            .ToListAsync(ct);

        var items = raw.Select(s => new ScreenSaverMasterViewModel
        {
            Code = s.Code,
            ImgUrl = s.ImageUrl,
            ClientCode = s.TenantId.ToString(),
            OrderNo = s.DisplayOrder.ToString(CultureInfo.InvariantCulture),
            IsActive = s.IsActive,
            CreatedOn = s.CreatedAt.UtcDateTime,
        }).ToList();

        return new ScreenSaverMasterList
        {
            screenSaverMasterList = items,
            Total = totals?.Total ?? 0,
            Active = totals?.Active ?? 0,
            InActive = totals?.InActive ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = filtered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(filtered / (double)pageSize) : 0,
        };
    }

    /// <summary>Add.</summary>
    public async Task<OperationResult> AddAsync(ScreenSaverMasterViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.Code) || string.IsNullOrWhiteSpace(vm.ImgUrl))
        {
            return OperationResult.Fail("Code and image URL are required.");
        }

        if (await db.ScreenSavers.AnyAsync(s => s.TenantId == tenantId && s.Code == vm.Code, ct))
        {
            return OperationResult.Fail($"Screen saver '{vm.Code}' already exists.");
        }

        db.ScreenSavers.Add(new ScreenSaver
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = vm.Code!,
            ImageUrl = vm.ImgUrl!,
            MediaType = vm.ImgUrl!.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ? "video" : "image",
            DisplayOrder = int.TryParse(vm.OrderNo, out var o) ? o : 0,
            IsActive = vm.IsActive,
        });
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit.</summary>
    public async Task<OperationResult> EditAsync(ScreenSaverMasterViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var s = await db.ScreenSavers.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == vm.Code, ct);
        if (s is null)
        {
            return OperationResult.Fail($"Screen saver '{vm.Code}' not found.");
        }

        if (!string.IsNullOrWhiteSpace(vm.ImgUrl))
        {
            s.ImageUrl = vm.ImgUrl!;
        }

        s.IsActive = vm.IsActive;
        if (int.TryParse(vm.OrderNo, out var o))
        {
            s.DisplayOrder = o;
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

        var s = await db.ScreenSavers.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);
        if (s is null)
        {
            return OperationResult.Fail($"Screen saver '{code}' not found.");
        }

        db.ScreenSavers.Remove(s);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }
}
