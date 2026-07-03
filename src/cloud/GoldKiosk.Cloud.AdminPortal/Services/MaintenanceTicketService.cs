using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Entities.Ops;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>Maintenance ticket service.</summary>
public sealed class MaintenanceTicketService(AppDbContext db, ICurrentUserService currentUser) : IMaintenanceTicketService
{
    private static readonly string[] AllowedStatuses = ["open", "in_progress", "resolved", "closed", "reopened", "other"];
    private static readonly string[] AllowedTypes = ["preventive", "corrective", "calibration", "cleaning"];

    /// <summary>List.</summary>
    public async Task<MaintenanceTicketList> ListAsync(string? search, string? status, string? ticketType, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return EmptyResult(pageSize, pageNo);
        }

        var q =
            from t in db.MaintenanceTickets.AsNoTracking().Where(x => x.TenantId == tenantId)
            join k in db.Kiosks.AsNoTracking() on t.KioskId equals k.Id into kj
            from k in kj.DefaultIfEmpty()
            join tech in db.Technicians.AsNoTracking() on t.TechnicianId equals tech.Id into tj
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

        if (!string.IsNullOrWhiteSpace(ticketType))
        {
            q = q.Where(x => x.t.TicketType == ticketType);
        }

        var totals = await db.MaintenanceTickets.AsNoTracking()
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
            .OrderByDescending(x => x.t.ScheduledAt ?? DateTimeOffset.MinValue)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(x => new
            {
                x.t.Id,
                x.t.Code,
                x.t.KioskId,
                x.KioskCode,
                x.t.DeviceId,
                x.t.TicketType,
                x.t.Description,
                x.t.Priority,
                x.t.Status,
                x.t.TechnicianId,
                x.TechName,
                x.t.ScheduledAt,
                x.t.CompletedAt,
            })
            .ToListAsync(ct);

        var items = raw.Select(x => new MaintenanceTicketViewModel
        {
            Id = x.Id,
            Code = x.Code,
            KioskId = x.KioskId,
            KioskCode = x.KioskCode,
            DeviceId = x.DeviceId,
            TicketType = x.TicketType,
            Description = x.Description,
            Priority = x.Priority,
            Status = x.Status,
            TechnicianId = x.TechnicianId,
            TechnicianName = x.TechName,
            ScheduledAt = x.ScheduledAt?.UtcDateTime,
            CompletedAt = x.CompletedAt?.UtcDateTime,
        }).ToList();

        return new MaintenanceTicketList
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
    public async Task<OperationResult> AddAsync(MaintenanceTicketViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        if (string.IsNullOrWhiteSpace(vm.Code))
        {
            return OperationResult.Fail("Ticket code is required.");
        }

        if (vm.KioskId == default)
        {
            return OperationResult.Fail("Kiosk is required.");
        }

        if (string.IsNullOrWhiteSpace(vm.Description))
        {
            return OperationResult.Fail("Description is required.");
        }

        if (!AllowedTypes.Contains(vm.TicketType))
        {
            vm.TicketType = "preventive";
        }

        var duplicate = await db.MaintenanceTickets.AnyAsync(t => t.TenantId == tenantId && t.Code == vm.Code, ct);
        if (duplicate)
        {
            return OperationResult.Fail($"Maintenance ticket '{vm.Code}' already exists.");
        }

        var t = new MaintenanceTicket
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = vm.Code,
            KioskId = vm.KioskId,
            DeviceId = vm.DeviceId,
            TicketType = vm.TicketType,
            Description = vm.Description,
            Priority = string.IsNullOrWhiteSpace(vm.Priority) ? "normal" : vm.Priority,
            Status = AllowedStatuses.Contains(vm.Status) ? vm.Status : "open",
            TechnicianId = vm.TechnicianId,
            ScheduledAt = vm.ScheduledAt.HasValue ? new DateTimeOffset(vm.ScheduledAt.Value, TimeSpan.Zero) : null,
        };

        db.MaintenanceTickets.Add(t);
        await db.SaveChangesAsync(ct);

        await AppendHistoryAsync(t.Id, null, t.Status, vm.Remarks ?? "Created", ct);
        return OperationResult.Ok();
    }

    /// <summary>Edit.</summary>
    public async Task<OperationResult> EditAsync(MaintenanceTicketViewModel vm, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return OperationResult.Fail("No tenant context.");
        }

        var t = await db.MaintenanceTickets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == vm.Id, ct);
        if (t is null)
        {
            return OperationResult.Fail("Maintenance ticket not found.");
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

        if (!string.IsNullOrWhiteSpace(vm.TicketType) && AllowedTypes.Contains(vm.TicketType))
        {
            t.TicketType = vm.TicketType;
        }

        if (!string.IsNullOrWhiteSpace(vm.Status) && AllowedStatuses.Contains(vm.Status))
        {
            t.Status = vm.Status;
        }

        t.TechnicianId = vm.TechnicianId;
        t.ScheduledAt = vm.ScheduledAt.HasValue ? new DateTimeOffset(vm.ScheduledAt.Value, TimeSpan.Zero) : t.ScheduledAt;
        if (t.Status is "closed" or "resolved")
        {
            t.CompletedAt ??= DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        if (oldStatus != t.Status)
        {
            await AppendHistoryAsync(t.Id, oldStatus, t.Status, vm.Remarks ?? "Edited", ct);
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

        var t = await db.MaintenanceTickets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        if (t is null)
        {
            return OperationResult.Fail("Maintenance ticket not found.");
        }

        db.MaintenanceTickets.Remove(t);
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

        var t = await db.MaintenanceTickets.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
        if (t is null)
        {
            return OperationResult.Fail("Maintenance ticket not found.");
        }

        var oldStatus = t.Status;
        if (oldStatus == newStatus)
        {
            return OperationResult.Ok();
        }

        t.Status = newStatus;
        if (newStatus is "closed" or "resolved")
        {
            t.CompletedAt ??= DateTimeOffset.UtcNow;
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
            TicketKind = "maintenance",
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedByUserId = currentUser.UserId,
            ChangedAt = DateTimeOffset.UtcNow,
            Remarks = remarks,
        });
        await db.SaveChangesAsync(ct);
    }

    private static MaintenanceTicketList EmptyResult(int pageSize, int pageNo) => new()
    {
        Items = new List<MaintenanceTicketViewModel>(),
        PageSize = pageSize,
        PageNo = pageNo,
    };
}
