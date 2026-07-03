using System.Globalization;
using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>Derived follow-up tasks (leads at active stages needing attention).</summary>
public class TasksController : Controller
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity for rep scoping.</param>
    public TasksController(CrmDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Lists leads needing follow-up, most stale first.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Tasks";
        ViewData["ActiveSection"] = "tasks";

        // Derived tasks: pull a small set of leads that need follow-up — anything
        // at the active stages. The schema doesn't have a tasks table yet; this is
        // the stub the prototype shows.
        var staleCutoff = DateTime.UtcNow.AddDays(-7).ToString("o", CultureInfo.InvariantCulture);

        // List<string> (not string[]) so EF binds Enumerable.Contains -> SQL IN
        // rather than the untranslatable .NET 10 ReadOnlySpan Contains overload.
        var activeStages = new List<string> { LeadStage.New, LeadStage.Contacted, LeadStage.Qualified, LeadStage.Proposal };

        var query = _db.Leads.AsNoTracking()
            .Where(l => l.DeletedAt == null && activeStages.Contains(l.Stage));

        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            query = query.Where(l => l.OwnerId == uid);
        }

        var leads = await query
            .OrderBy(l => l.UpdatedAt)
            .Take(50)
            .ToListAsync(ct);

        // Surface proposal-stage + the most-stale first.
        var ordered = leads
            .OrderBy(l => l.Stage == LeadStage.Proposal ? 0 : 1)
            .ThenBy(l => l.UpdatedAt)
            .ToList();

        ViewData["StaleCutoff"] = staleCutoff;
        return View(ordered);
    }
}
