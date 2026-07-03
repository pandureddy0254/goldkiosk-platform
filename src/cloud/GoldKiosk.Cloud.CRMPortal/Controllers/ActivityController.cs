using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>Recent lead activity feed (rep-scoped).</summary>
public class ActivityController : Controller
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity for rep scoping.</param>
    public ActivityController(CrmDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Lists the 100 most recent activities visible to the caller.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Activity";
        ViewData["ActiveSection"] = "activity";

        // Rep scoping: managers see all activities; a rep sees activities only on
        // leads they own (mirrors lead_activities_app_select).
        var query = _db.LeadActivities.AsNoTracking().AsQueryable();
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            query = query.Where(a => _db.Leads.Any(l => l.Id == a.LeadId && l.OwnerId == uid));
        }

        var activities = await query
            .OrderByDescending(a => a.OccurredAt)
            .Take(100)
            .ToListAsync(ct);

        var leadLookup = new Dictionary<Guid, Lead>();
        if (activities.Count > 0)
        {
            var leadIds = activities.Select(a => a.LeadId).Distinct().ToList();
            leadLookup = await _db.Leads.AsNoTracking()
                .Where(l => leadIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, ct);
        }

        ViewData["LeadLookup"] = leadLookup;
        return View(activities);
    }
}
