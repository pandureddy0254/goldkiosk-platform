using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Infrastructure.Common;
using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Services;

/// <summary>SOS service.</summary>
public sealed class SosService(AppDbContext db, ICurrentUserService currentUser) : ISosService
{
    /// <summary>List.</summary>
    public async Task<SosRequestList> ListAsync(string? status, int pageSize, int pageNo, CancellationToken ct = default)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return Empty(pageSize, pageNo);
        }

        var q = db.SosRequests.AsNoTracking().Where(s => s.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            q = q.Where(s => s.Status == status);
        }

        var totals = await db.SosRequests.AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Open = g.Count(s => s.Status == "raised" || s.Status == "acknowledged" || s.Status == "dispatched"),
                Resolved = g.Count(s => s.Status == "resolved"),
            })
            .FirstOrDefaultAsync(ct);

        var totalFiltered = await q.CountAsync(ct);

        // Two-step projection: keep DateTimeOffset native in SQL, convert client-side.
        var raw = await q
            .OrderByDescending(s => s.RaisedAt)
            .Skip(Math.Max(0, (pageNo - 1) * pageSize))
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.KioskId,
                s.CustomerId,
                s.Priority,
                s.Status,
                s.Description,
                s.AssignedTechnicianId,
                s.RaisedAt,
                s.AcknowledgedAt,
                s.ResolvedAt,
            })
            .ToListAsync(ct);

        var items = raw.Select(s => new SosRequestViewModel
        {
            Id = s.Id,
            KioskId = s.KioskId,
            CustomerId = s.CustomerId,
            Priority = s.Priority,
            Status = s.Status,
            Description = s.Description,
            AssignedTechnicianId = s.AssignedTechnicianId,
            RaisedAt = s.RaisedAt.UtcDateTime,
            AcknowledgedAt = s.AcknowledgedAt?.UtcDateTime,
            ResolvedAt = s.ResolvedAt?.UtcDateTime,
        }).ToList();

        return new SosRequestList
        {
            Items = items,
            Total = totals?.Total ?? 0,
            Open = totals?.Open ?? 0,
            Resolved = totals?.Resolved ?? 0,
            PageSize = pageSize,
            PageNo = pageNo,
            PageCount = totalFiltered,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling(totalFiltered / (double)pageSize) : 0,
        };
    }

    /// <summary>Acknowledge.</summary>
    public async Task<OperationResult> AcknowledgeAsync(Guid id, CancellationToken ct = default)
    {
        var (sos, err) = await LoadAsync(id, ct);
        if (sos is null)
        {
            return OperationResult.Fail(err!);
        }

        if (sos.Status != "raised")
        {
            return OperationResult.Fail($"SOS is in status '{sos.Status}' and cannot be acknowledged.");
        }

        sos.Status = "acknowledged";
        sos.AcknowledgedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Dispatch.</summary>
    public async Task<OperationResult> DispatchAsync(Guid id, Guid technicianId, CancellationToken ct = default)
    {
        if (technicianId == Guid.Empty)
        {
            return OperationResult.Fail("Technician is required.");
        }

        var (sos, err) = await LoadAsync(id, ct);
        if (sos is null)
        {
            return OperationResult.Fail(err!);
        }

        if (sos.Status is not ("raised" or "acknowledged"))
        {
            return OperationResult.Fail($"SOS in status '{sos.Status}' cannot be dispatched.");
        }

        sos.Status = "dispatched";
        sos.AssignedTechnicianId = technicianId;
        sos.AcknowledgedAt ??= DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    /// <summary>Resolve.</summary>
    public async Task<OperationResult> ResolveAsync(Guid id, CancellationToken ct = default)
    {
        var (sos, err) = await LoadAsync(id, ct);
        if (sos is null)
        {
            return OperationResult.Fail(err!);
        }

        if (sos.Status == "resolved" || sos.Status == "cancelled")
        {
            return OperationResult.Fail($"SOS already in terminal status '{sos.Status}'.");
        }

        sos.Status = "resolved";
        sos.ResolvedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    private async Task<(GoldKiosk.Infrastructure.Entities.Helpdesk.SosRequest? Entity, string? Error)> LoadAsync(Guid id, CancellationToken ct)
    {
        if (currentUser.TenantId is not Guid tenantId)
        {
            return (null, "No tenant context.");
        }

        var sos = await db.SosRequests.FirstOrDefaultAsync(s => s.Id == id && s.TenantId == tenantId, ct);
        return sos is null ? (null, $"SOS '{id}' not found.") : (sos, null);
    }

    private static SosRequestList Empty(int pageSize, int pageNo) => new()
    {
        Items = new List<SosRequestViewModel>(),
        Total = 0,
        Open = 0,
        Resolved = 0,
        PageSize = pageSize,
        PageNo = pageNo,
        PageCount = 0,
        TotalPage = 0,
    };
}
