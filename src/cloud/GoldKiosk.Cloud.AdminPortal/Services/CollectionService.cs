using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Ops;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Collection service.</summary>
public sealed class CollectionService(AppDbContext db, ICurrentUserService currentUser) : ICollectionService
{
    private static readonly string[] AllowedRunStatuses = ["planned", "in_progress", "completed", "cancelled"];
    private static readonly string[] AllowedTicketStatuses = ["pending", "collected", "skipped", "discrepancy"];

    /// <summary>List.</summary>
    public async Task<CollectionTicketList> ListAsync(string? search, string? status, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return EmptyResult(pageSize, pageNo);
        }

        var runsQ =
            from r in db.CollectionRuns.AsNoTracking().Where(x => x.TenantId == tenantId)
            join tech in db.Technicians.AsNoTracking() on r.LeadTechnicianId equals tech.Id into tj
            from tech in tj.DefaultIfEmpty()
#pragma warning disable IDE0031 // null propagation is illegal inside expression trees (CS8072)
            select new { r, TechName = tech == null ? null : tech.Name };
#pragma warning restore IDE0031

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            runsQ = runsQ.Where(x => EF.Functions.ILike(x.r.Code, s));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            runsQ = runsQ.Where(x => x.r.Status == status);
        }

        var total = await runsQ.CountAsync(ct);

        // Two-step projection: shaper cannot coerce DateTimeOffset -> Nullable<DateTime> inside a LEFT JOIN shape.
        var runsRaw = await runsQ
            .OrderByDescending(x => x.r.RunDate)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(x => new
            {
                x.r.Id,
                x.r.Code,
                x.r.RunDate,
                x.r.LeadTechnicianId,
                x.TechName,
                x.r.Status,
                x.r.TotalAmount,
                x.r.TotalItems,
                x.r.StartedAt,
                x.r.CompletedAt,
            })
            .ToListAsync(ct);

        var runs = runsRaw.Select(x => new CollectionRunViewModel
        {
            Id = x.Id,
            Code = x.Code,
            RunDate = x.RunDate.ToDateTime(TimeOnly.MinValue),
            LeadTechnicianId = x.LeadTechnicianId,
            LeadTechnicianName = x.TechName,
            Status = x.Status,
            TotalAmount = x.TotalAmount,
            TotalItems = x.TotalItems,
            StartedAt = x.StartedAt?.UtcDateTime,
            CompletedAt = x.CompletedAt?.UtcDateTime,
        }).ToList();

        var runIds = runs.Select(r => r.Id).ToList();
        var ticketsQ =
            from t in db.CollectionTickets.AsNoTracking().Where(x => runIds.Contains(x.CollectionRunId))
            join k in db.Kiosks.AsNoTracking() on t.KioskId equals k.Id into kj
            from k in kj.DefaultIfEmpty()
            join r in db.CollectionRuns.AsNoTracking() on t.CollectionRunId equals r.Id
#pragma warning disable IDE0031 // null propagation is illegal inside expression trees (CS8072)
            select new { t, KioskCode = k == null ? null : k.Code, RunCode = r.Code };
#pragma warning restore IDE0031

        // Two-step projection: shaper cannot coerce DateTimeOffset -> Nullable<DateTime> inside a LEFT JOIN shape.
        var ticketsRaw = await ticketsQ
            .OrderByDescending(x => x.t.CollectedAt ?? DateTimeOffset.MinValue)
            .Select(x => new
            {
                x.t.Id,
                x.t.CollectionRunId,
                x.RunCode,
                x.t.KioskId,
                x.KioskCode,
                x.t.AmountCollected,
                x.t.CurrencyCode,
                x.t.ItemsCollected,
                x.t.Status,
                x.t.CollectedAt,
                x.t.SignedOffByUserId,
            })
            .ToListAsync(ct);

        var tickets = ticketsRaw.Select(x => new CollectionTicketViewModel
        {
            Id = x.Id,
            CollectionRunId = x.CollectionRunId,
            RunCode = x.RunCode,
            KioskId = x.KioskId,
            KioskCode = x.KioskCode,
            AmountCollected = x.AmountCollected,
            CurrencyCode = x.CurrencyCode,
            ItemsCollected = x.ItemsCollected,
            Status = x.Status,
            CollectedAt = x.CollectedAt?.UtcDateTime,
            SignedOffByUserId = x.SignedOffByUserId,
        }).ToList();

        return new CollectionTicketList
        {
            Runs = runs,
            Tickets = tickets,
            Total = total,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = total,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0,
        };
    }

    /// <summary>Add run.</summary>
    public async Task<OperationResult> AddRunAsync(CollectionRunViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.Code))
        {
            return OperationResult.Fail("Run code is required.");
        }

        if (!AllowedRunStatuses.Contains(vm.Status))
        {
            vm.Status = "planned";
        }

        var duplicate = await db.CollectionRuns.AnyAsync(r => r.TenantId == tenantId && r.Code == vm.Code, ct);
        if (duplicate)
        {
            return OperationResult.Fail($"Collection run '{vm.Code}' already exists.");
        }

        var r = new CollectionRun
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = vm.Code,
            RunDate = vm.RunDate == default ? DateOnly.FromDateTime(DateTime.UtcNow) : DateOnly.FromDateTime(vm.RunDate),
            LeadTechnicianId = vm.LeadTechnicianId,
            Status = vm.Status,
            TotalAmount = vm.TotalAmount,
            TotalItems = vm.TotalItems,
        };
        db.CollectionRuns.Add(r);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit run.</summary>
    public async Task<OperationResult> EditRunAsync(CollectionRunViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var r = await db.CollectionRuns.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == vm.Id, ct);
        if (r is null)
        {
            return OperationResult.Fail("Collection run not found.");
        }

        if (vm.RunDate != default)
        {
            r.RunDate = DateOnly.FromDateTime(vm.RunDate);
        }

        r.LeadTechnicianId = vm.LeadTechnicianId;
        if (!string.IsNullOrWhiteSpace(vm.Status) && AllowedRunStatuses.Contains(vm.Status))
        {
            r.Status = vm.Status;
            if (vm.Status == "in_progress")
            {
                r.StartedAt ??= DateTimeOffset.UtcNow;
            }

            if (vm.Status == "completed")
            {
                r.CompletedAt ??= DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Delete run.</summary>
    public async Task<OperationResult> DeleteRunAsync(Guid id, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var r = await db.CollectionRuns.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        if (r is null)
        {
            return OperationResult.Fail("Collection run not found.");
        }

        db.CollectionRuns.Remove(r);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Add ticket.</summary>
    public async Task<OperationResult> AddTicketAsync(CollectionTicketViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (vm.CollectionRunId == default)
        {
            return OperationResult.Fail("Collection run is required.");
        }

        if (vm.KioskId == default)
        {
            return OperationResult.Fail("Kiosk is required.");
        }

        if (string.IsNullOrWhiteSpace(vm.CurrencyCode))
        {
            return OperationResult.Fail("Currency is required.");
        }

        // Defence-in-depth: confirm both the run and the kiosk belong to the caller's tenant.
        var runOk = await db.CollectionRuns.AnyAsync(r => r.Id == vm.CollectionRunId && r.TenantId == tenantId, ct);
        if (!runOk)
        {
            return OperationResult.Fail("Collection run does not belong to this tenant.");
        }

        var kioskOk = await db.Kiosks.AnyAsync(k => k.Id == vm.KioskId && k.TenantId == tenantId, ct);
        if (!kioskOk)
        {
            return OperationResult.Fail("Kiosk does not belong to this tenant.");
        }

        var t = new CollectionTicket
        {
            Id = Guid.NewGuid(),
            CollectionRunId = vm.CollectionRunId,
            KioskId = vm.KioskId,
            AmountCollected = vm.AmountCollected,
            CurrencyCode = vm.CurrencyCode,
            ItemsCollected = vm.ItemsCollected,
            Status = AllowedTicketStatuses.Contains(vm.Status) ? vm.Status : "pending",
            CollectedAt = vm.Status == "collected" ? DateTimeOffset.UtcNow : (vm.CollectedAt.HasValue ? new DateTimeOffset(vm.CollectedAt.Value, TimeSpan.Zero) : null),
        };
        db.CollectionTickets.Add(t);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit ticket.</summary>
    public async Task<OperationResult> EditTicketAsync(CollectionTicketViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        // Join to runs to enforce tenant scope.
        var t = await (from x in db.CollectionTickets
                       join r in db.CollectionRuns on x.CollectionRunId equals r.Id
                       where x.Id == vm.Id && r.TenantId == tenantId
                       select x).FirstOrDefaultAsync(ct);
        if (t is null)
        {
            return OperationResult.Fail("Collection ticket not found.");
        }

        t.AmountCollected = vm.AmountCollected;
        t.ItemsCollected = vm.ItemsCollected;
        if (!string.IsNullOrWhiteSpace(vm.CurrencyCode))
        {
            t.CurrencyCode = vm.CurrencyCode;
        }

        if (!string.IsNullOrWhiteSpace(vm.Status) && AllowedTicketStatuses.Contains(vm.Status))
        {
            t.Status = vm.Status;
            if (vm.Status == "collected")
            {
                t.CollectedAt ??= DateTimeOffset.UtcNow;
            }
        }

        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Sign off.</summary>
    public async Task<OperationResult> SignOffAsync(Guid ticketId, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (currentUser.UserId is not Guid userId)
        {
            return OperationResult.Fail("No signed-in user to sign off as.");
        }

        var t = await (from x in db.CollectionTickets
                       join r in db.CollectionRuns on x.CollectionRunId equals r.Id
                       where x.Id == ticketId && r.TenantId == tenantId
                       select x).FirstOrDefaultAsync(ct);
        if (t is null)
        {
            return OperationResult.Fail("Collection ticket not found.");
        }

        t.SignedOffByUserId = userId;
        if (t.Status == "pending")
        {
            t.Status = "collected";
        }

        t.CollectedAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    private static CollectionTicketList EmptyResult(int pageSize, int pageNo) => new()
    {
        Runs = new List<CollectionRunViewModel>(),
        Tickets = new List<CollectionTicketViewModel>(),
        PageSize = pageSize,
        PageNo = pageNo,
    };
}
