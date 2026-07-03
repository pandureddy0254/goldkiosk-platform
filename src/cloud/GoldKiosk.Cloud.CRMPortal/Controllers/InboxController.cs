using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>Inbound signals feed (email + call activities on leads).</summary>
public class InboxController : Controller
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity for rep scoping.</param>
    public InboxController(CrmDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Lists the last 30 days of inbound email/call activities.</summary>
    /// <param name="source">"call", "email", or anything else for both.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(string? source, CancellationToken ct)
    {
        ViewData["Title"] = "Inbox";
        ViewData["ActiveSection"] = "inbox";

        // Inbound signals = email + call activities on leads (closest analogue we
        // have until SES inbound + the public contact form land in their own table).
        var since = DateTime.UtcNow.AddDays(-30);

        var query = _db.LeadActivities.AsNoTracking()
            .Where(a => a.OccurredAt >= since);

        if (string.Equals(source, "call", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(a => a.Type == ActivityType.Call);
        }
        else if (string.Equals(source, "email", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(a => a.Type == ActivityType.Email);
        }
        else
        {
            query = query.Where(a => a.Type == ActivityType.Email || a.Type == ActivityType.Call);
        }

        // Rep scoping: managers see all; a rep sees activities on leads they own.
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            query = query.Where(a => _db.Leads.Any(l => l.Id == a.LeadId && l.OwnerId == uid));
        }

        var acts = await query
            .OrderByDescending(a => a.OccurredAt)
            .Take(50)
            .ToListAsync(ct);

        var leads = new Dictionary<Guid, Lead>();
        if (acts.Count > 0)
        {
            var leadIds = acts.Select(a => a.LeadId).Distinct().ToList();
            leads = await _db.Leads.AsNoTracking()
                .Where(l => leadIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id, ct);
        }

        ViewData["LeadLookup"] = leads;
        ViewData["SourceFilter"] = source ?? "all";
        return View(acts);
    }
}
