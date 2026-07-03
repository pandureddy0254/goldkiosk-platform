using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Ops;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Deployment ticket service.</summary>
public sealed class DeploymentTicketService(AppDbContext db, ICurrentUserService currentUser) : IDeploymentTicketService
{
    private static readonly string[] AllowedStatuses = ["open", "in_progress", "resolved", "closed", "reopened", "other"];

    /// <summary>List.</summary>
    public async Task<DeploymentTicketList> ListAsync(string? search, string? status, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return EmptyResult(pageSize, pageNo);
        }

        var q =
            from t in db.DeploymentTickets.AsNoTracking().Where(x => x.TenantId == tenantId)
            join k in db.Kiosks.AsNoTracking() on t.KioskId equals k.Id into kj
            from k in kj.DefaultIfEmpty()
            join tech in db.Technicians.AsNoTracking() on t.AssignedTechnicianId equals tech.Id into tj
            from tech in tj.DefaultIfEmpty()
#pragma warning disable IDE0031 // null propagation is illegal inside expression trees (CS8072)
            select new { t, KioskCode = k == null ? null : k.Code, TechName = tech == null ? null : tech.Name };
#pragma warning restore IDE0031

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            q = q.Where(x => EF.Functions.ILike(x.t.Code, s) || EF.Functions.ILike(x.t.Description, s));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            q = q.Where(x => x.t.Status == status);
        }

        var totals = await db.DeploymentTickets.AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Open = g.Count(t => t.Status == "open"),
                InProgress = g.Count(t => t.Status == "in_progress"),
                Closed = g.Count(t => t.Status == "closed"),
                Resolved = g.Count(t => t.Status == "resolved"),
                Reopened = g.Count(t => t.Status == "reopened"),
                Others = g.Count(t => t.Status == "other"),
            })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await q.CountAsync(ct);

        // Two-step projection: shaper cannot coerce DateTimeOffset -> Nullable<DateTime> inside a LEFT JOIN shape.
        var raw = await q
            .OrderByDescending(x => x.t.OpenedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(x => new
            {
                x.t.Id,
                x.t.Code,
                x.t.KioskId,
                x.KioskCode,
                x.t.StoreId,
                x.t.Status,
                x.t.Priority,
                x.t.Description,
                x.t.AssignedTechnicianId,
                x.TechName,
                x.t.OpenedAt,
                x.t.ClosedAt,
            })
            .ToListAsync(ct);

        var items = raw.Select(x => new DeploymentTicketViewModel
        {
            Id = x.Id,
            Code = x.Code,
            KioskId = x.KioskId,
            KioskCode = x.KioskCode,
            StoreId = x.StoreId,
            Status = x.Status,
            Priority = x.Priority,
            Description = x.Description,
            AssignedTechnicianId = x.AssignedTechnicianId,
            TechnicianName = x.TechName,
            OpenedAt = x.OpenedAt.UtcDateTime,
            ClosedAt = x.ClosedAt?.UtcDateTime,
        }).ToList();

        return new DeploymentTicketList
        {
            Items = items,
            Total = totals?.Total ?? 0,
            Open = totals?.Open ?? 0,
            InProgress = totals?.InProgress ?? 0,
            Closed = totals?.Closed ?? 0,
            Resolved = totals?.Resolved ?? 0,
            Reopened = totals?.Reopened ?? 0,
            Others = totals?.Others ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    /// <summary>Add.</summary>
    public async Task<OperationResult> AddAsync(DeploymentTicketViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.Code))
        {
            return OperationResult.Fail("Ticket code is required.");
        }

        if (string.IsNullOrWhiteSpace(vm.Description))
        {
            return OperationResult.Fail("Description is required.");
        }

        if (!AllowedStatuses.Contains(vm.Status))
        {
            vm.Status = "open";
        }

        var duplicate = await db.DeploymentTickets.AnyAsync(t => t.TenantId == tenantId && t.Code == vm.Code, ct);
        if (duplicate)
        {
            return OperationResult.Fail($"Deployment ticket '{vm.Code}' already exists.");
        }

        var t = new DeploymentTicket
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = vm.Code,
            KioskId = vm.KioskId,
            StoreId = vm.StoreId,
            Status = vm.Status,
            Priority = string.IsNullOrWhiteSpace(vm.Priority) ? "normal" : vm.Priority,
            Description = vm.Description,
            AssignedTechnicianId = vm.AssignedTechnicianId,
            OpenedAt = DateTimeOffset.UtcNow,
        };

        db.DeploymentTickets.Add(t);
        await db.SaveChangesAsync(ct);

        await AppendHistoryAsync(t.Id, null, t.Status, "Created", ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit.</summary>
    public async Task<OperationResult> EditAsync(DeploymentTicketViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var t = await db.DeploymentTickets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == vm.Id, ct);
        if (t is null)
        {
            return OperationResult.Fail("Deployment ticket not found.");
        }

        var oldStatus = t.Status;
        if (!string.IsNullOrWhiteSpace(vm.Description))
        {
            t.Description = vm.Description;
        }

        if (!string.IsNullOrWhiteSpace(vm.Priority))
        {
            t.Priority = vm.Priority;
        }

        if (!string.IsNullOrWhiteSpace(vm.Status) && AllowedStatuses.Contains(vm.Status))
        {
            t.Status = vm.Status;
        }

        t.KioskId = vm.KioskId;
        t.StoreId = vm.StoreId;
        t.AssignedTechnicianId = vm.AssignedTechnicianId;
        if (t.Status is "closed" or "resolved")
        {
            t.ClosedAt ??= DateTimeOffset.UtcNow;
        }

        if (t.Status is "open" or "reopened" or "in_progress")
        {
            t.ClosedAt = null;
        }

        await db.SaveChangesAsync(ct);
        if (oldStatus != t.Status)
        {
            await AppendHistoryAsync(t.Id, oldStatus, t.Status, "Edited", ct);
        }

        return OperationResult.Ok();
    }

    /// <summary>Delete.</summary>
    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var t = await db.DeploymentTickets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        if (t is null)
        {
            return OperationResult.Fail("Deployment ticket not found.");
        }

        db.DeploymentTickets.Remove(t);
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Change status.</summary>
    public async Task<OperationResult> ChangeStatusAsync(Guid id, string newStatus, string? remarks, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (!AllowedStatuses.Contains(newStatus))
        {
            return OperationResult.Fail($"Status '{newStatus}' is not allowed.");
        }

        var t = await db.DeploymentTickets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        if (t is null)
        {
            return OperationResult.Fail("Deployment ticket not found.");
        }

        var oldStatus = t.Status;
        if (oldStatus == newStatus)
        {
            return OperationResult.Ok();
        }

        t.Status = newStatus;
        if (newStatus is "closed" or "resolved")
        {
            t.ClosedAt ??= DateTimeOffset.UtcNow;
        }

        if (newStatus is "open" or "reopened" or "in_progress")
        {
            t.ClosedAt = null;
        }

        await db.SaveChangesAsync(ct);
        await AppendHistoryAsync(t.Id, oldStatus, newStatus, remarks, ct);
        return OperationResult.Ok();
    }

    private async Task AppendHistoryAsync(Guid ticketId, string? oldStatus, string newStatus, string? remarks, CancellationToken ct)
    {
        db.TicketStatusHistory.Add(new TicketStatusHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            TicketKind = "deployment",
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = currentUser.UserId,
            ChangedAt = DateTimeOffset.UtcNow,
            Remarks = remarks,
        });
        await db.SaveChangesAsync(ct);
    }

    private static DeploymentTicketList EmptyResult(int pageSize, int pageNo) => new()
    {
        Items = new List<DeploymentTicketViewModel>(),
        PageSize = pageSize,
        PageNo = pageNo,
    };
}
