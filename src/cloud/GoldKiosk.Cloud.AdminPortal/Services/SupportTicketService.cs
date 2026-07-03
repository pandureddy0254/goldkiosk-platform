using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Helpdesk;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Support ticket service.</summary>
public sealed class SupportTicketService(AppDbContext db, ICurrentUserService currentUser) : ISupportTicketService
{
    /// <summary>List.</summary>
    public async Task<SupportTicketMasterList> ListAsync(
        string? search,
        string? feature,
        string? type,
        string? status,
        string? categoryCode,
        string? subCategoryCode,
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

        // Join to category lookup so we can filter by code and project names.
        var baseQuery =
            from t in db.SupportTickets.AsNoTracking().Where(t => t.TenantId == tenantId)
            join c in db.TicketCategories.AsNoTracking() on t.CategoryId equals c.Id
            join sc in db.TicketSubCategories.AsNoTracking() on t.SubCategoryId equals sc.Id into scj
            from sc in scj.DefaultIfEmpty()
            select new { t, c, sc };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            baseQuery = baseQuery.Where(x => EF.Functions.ILike(x.t.Code, s)
                                          || EF.Functions.ILike(x.t.Description, s));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            baseQuery = baseQuery.Where(x => x.t.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(categoryCode))
        {
            baseQuery = baseQuery.Where(x => x.c.Code == categoryCode);
        }

        if (!string.IsNullOrWhiteSpace(subCategoryCode))
        {
            baseQuery = baseQuery.Where(x => x.sc != null && x.sc.Code == subCategoryCode);
        }

        // `feature` / `type` are kept in the API surface for view-compatibility,
        // but the schema has no matching columns yet — they are effectively no-ops
        // until a 0034 migration adds them. Do not silently drop the parameters.
        _ = feature;
        _ = type;

        if (startDate is { } sd)
        {
            baseQuery = baseQuery.Where(x => x.t.CreatedAt >= new DateTimeOffset(sd, TimeSpan.Zero));
        }

        if (endDate is { } ed)
        {
            baseQuery = baseQuery.Where(x => x.t.CreatedAt <= new DateTimeOffset(ed.AddDays(1), TimeSpan.Zero));
        }

        var totals = await db.SupportTickets.AsNoTracking()
            .Where(t => t.TenantId == tenantId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Open = g.Count(t => !t.IsClosed),
                Closed = g.Count(t => t.IsClosed),
            })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await baseQuery.CountAsync(ct);

        // Two-step projection: LEFT JOIN + .UtcDateTime conversions break EF's shaper.
        var raw = await baseQuery
            .OrderByDescending(x => x.t.CreatedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(x => new
            {
                x.t.Code,
                CategoryCode = x.c.Code,
                CategoryName = x.c.Name,
                SubCategoryCode = x.sc != null ? x.sc.Code : string.Empty,
                SubCategoryName = x.sc != null ? x.sc.Name : string.Empty,
                x.t.Description,
                x.t.Status,
                x.t.Remarks,
                x.t.DocumentsUri,
                x.t.IsClosed,
                x.t.IsReopened,
                x.t.CreatedAt,
                x.t.ClosedAt,
                x.t.ReopenedAt,
                x.t.TenantId,
            })
            .ToListAsync(ct);

        var items = raw.Select(x => new SupportTicketViewModel
        {
            Code = x.Code,
            CategoryCode = x.CategoryCode,
            CategoryName = x.CategoryName,
            SubCategoryCode = x.SubCategoryCode,
            SubCategoryName = x.SubCategoryName,
            Description = x.Description,
            Status = x.Status,
            Remarks = x.Remarks ?? string.Empty,
            Documents = x.DocumentsUri ?? string.Empty,
            IsActive = !x.IsClosed,
            IsClosed = x.IsClosed,
            IsReopened = x.IsReopened,
            CreatedOn = x.CreatedAt.UtcDateTime,
            UpdatedOn = (x.ReopenedAt ?? x.ClosedAt ?? x.CreatedAt).UtcDateTime,
            ClosedOn = x.ClosedAt?.UtcDateTime,
            ReopenedOn = x.ReopenedAt?.UtcDateTime,
            ClientCode = x.TenantId.ToString(),
        }).ToList();

        return new SupportTicketMasterList
        {
            SupportTicketItems = items,
            Total = totals?.Total ?? 0,
            Active = totals?.Open ?? 0,
            InActive = totals?.Closed ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    /// <summary>Get categories.</summary>
    public async Task<List<SelectListItem>> GetCategoriesAsync(CancellationToken ct = default)
    {
        return await db.TicketCategories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem { Value = c.Code, Text = c.Name })
            .ToListAsync(ct);
    }

    /// <summary>Get subcategories.</summary>
    public async Task<IReadOnlyList<SubCategoryDto>> GetSubcategoriesAsync(string parentCategoryCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(parentCategoryCode))
        {
            return Array.Empty<SubCategoryDto>();
        }

        var rows = await (
            from sc in db.TicketSubCategories.AsNoTracking()
            join c in db.TicketCategories.AsNoTracking() on sc.ParentCategoryId equals c.Id
            where c.Code == parentCategoryCode && sc.IsActive
            orderby sc.Name
            select new SubCategoryDto(sc.Code, sc.Name)
        ).ToListAsync(ct);

        return rows;
    }

    /// <summary>Add.</summary>
    public async Task<OperationResult> AddAsync(SupportTicketViewModel vm, string? documentsUri, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (currentUser.UserId is not Guid userId)
        {
            return OperationResult.Fail("Not authenticated.");
        }

        if (string.IsNullOrWhiteSpace(vm.CategoryCode))
        {
            return OperationResult.Fail("Category is required.");
        }

        if (string.IsNullOrWhiteSpace(vm.Description))
        {
            return OperationResult.Fail("Description is required.");
        }

        var category = await db.TicketCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == vm.CategoryCode, ct);
        if (category is null)
        {
            return OperationResult.Fail($"Category '{vm.CategoryCode}' not found.");
        }

        Guid? subCategoryId = null;
        if (!string.IsNullOrWhiteSpace(vm.SubCategoryCode))
        {
            var sub = await db.TicketSubCategories.AsNoTracking()
                .FirstOrDefaultAsync(sc => sc.ParentCategoryId == category.Id && sc.Code == vm.SubCategoryCode, ct);
            if (sub is null)
            {
                return OperationResult.Fail($"Sub-category '{vm.SubCategoryCode}' not found under '{vm.CategoryCode}'.");
            }

            subCategoryId = sub.Id;
        }

        // TKT-YYYYMMDD-NNNN. Full gap-less sequence comes in a later pass; for now
        // a 4-digit random suffix is sufficient and avoids most collisions per day.
        var code = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(0, 10000):D4}";

        var entity = new SupportTicket
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            CategoryId = category.Id,
            SubCategoryId = subCategoryId,
            Description = vm.Description,
            Status = "open",
            Remarks = string.IsNullOrWhiteSpace(vm.Remarks) ? null : vm.Remarks,
            DocumentsUri = documentsUri,
            CreatedByUserId = userId,
            IsClosed = false,
            IsReopened = false,
        };

        db.SupportTickets.Add(entity);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    private static SupportTicketMasterList Empty(int pageSize, int pageNo) => new()
    {
        SupportTicketItems = new List<SupportTicketViewModel>(),
        Total = 0,
        Active = 0,
        InActive = 0,
        PageSize = pageSize,
        PageNo = pageNo,
        PageCount = 0,
        TotalPage = 0,
    };
}
