using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Infrastructure;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>Manager-only view over the append-only audit trail.</summary>
[RequireManager]
public class AuditLogController : Controller
{
    private readonly CrmDbContext _db;

    /// <summary>Initializes the controller with the CRM database context.</summary>
    /// <param name="db">CRM database context.</param>
    public AuditLogController(CrmDbContext db) => _db = db;

    /// <summary>
    /// Lists the latest audit entries. NOTE: parameter is <c>actionType</c> NOT <c>action</c> —
    /// the query-string name <c>action</c> collides with MVC's route token {action}, which
    /// always equals the method name ("Index"). The form uses name="actionType" too.
    /// </summary>
    /// <param name="q">Free-text filter over actor name/email and entity id.</param>
    /// <param name="entity">Entity-type filter or "all".</param>
    /// <param name="actionType">Action filter or "all".</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(string? q, string? entity, string? actionType, CancellationToken ct)
    {
        ViewData["Title"] = "Audit log";
        ViewData["ActiveSection"] = "audit-log";

        // Pass filter values back so the form re-hydrates on submit.
        ViewBag.Q = q;
        ViewBag.Entity = entity;
        ViewBag.ActionType = actionType;

        var query = _db.AuditLog.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entity) && !string.Equals(entity, "all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(e => e.EntityType == entity);
        }

        if (!string.IsNullOrWhiteSpace(actionType) && !string.Equals(actionType, "all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(e => e.Action == actionType);
        }

        var entries = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        // Resolve actor profiles so the view can show names/emails.
        var actorLookup = new Dictionary<Guid, Profile>();
        var actorIds = entries
            .Where(e => e.ActorId.HasValue)
            .Select(e => e.ActorId!.Value)
            .Distinct()
            .ToList();
        if (actorIds.Count > 0)
        {
            actorLookup = await _db.Profiles.AsNoTracking()
                .Where(p => actorIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, ct);
        }

        // Apply text filter client-side over actor name/email and entity_id.
        if (!string.IsNullOrWhiteSpace(q))
        {
            entries = entries.Where(e =>
            {
                if (e.EntityId?.ToString().Contains(q, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return true;
                }

                if (e.ActorId.HasValue && actorLookup.TryGetValue(e.ActorId.Value, out var actor))
                {
                    if (actor.Email?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return true;
                    }

                    if (actor.FullName?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return true;
                    }
                }

                return false;
            }).ToList();
        }

        ViewData["ActorLookup"] = actorLookup;
        return View(entries);
    }
}
