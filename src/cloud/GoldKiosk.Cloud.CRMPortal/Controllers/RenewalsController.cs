using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>
/// Renewal reminders board (next 60 days).
/// TODO(GK-CRM-1): the daily 02:00 renewal-reminder cron the view copy references
/// (SES drain of billing.renewal_reminders) is not ported yet — reminders stay
/// "queued" until that job lands.
/// </summary>
public class RenewalsController : Controller
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity for rep scoping.</param>
    public RenewalsController(CrmDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Lists reminders scheduled in the next 60 days, grouped by delivery status.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["ActiveSection"] = "renewals";

        // Pull next 60 days of reminders (scheduled_for is a DATE column).
        var horizon = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)).ToDateTime(TimeOnly.MinValue);

        var query = _db.RenewalReminders.AsNoTracking()
            .Where(r => r.ScheduledFor <= horizon);

        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            query = query.Where(r => _db.Subscriptions.Any(s => s.Id == r.SubscriptionId
                && _db.Partners.Any(p => p.Id == s.PartnerId
                    && _db.Leads.Any(l => l.Id == p.LeadId && l.OwnerId == uid))));
        }

        var reminders = await query
            .OrderBy(r => r.ScheduledFor)
            .Take(200)
            .ToListAsync(ct);

        // Subscription -> partner display
        var subById = await ScopedSubscriptions().ToDictionaryAsync(s => s.Id, s => s, ct);
        var partners = await ScopedPartners().ToDictionaryAsync(p => p.Id, p => p, ct);

        ViewBag.Grouped = reminders.GroupBy(r => r.DeliveryStatus).ToDictionary(g => g.Key, g => g.ToList());
        ViewBag.SubById = subById;
        ViewBag.Partners = partners;
        return View(reminders);
    }

    private IQueryable<Subscription> ScopedSubscriptions()
    {
        var q = _db.Subscriptions.AsNoTracking().AsQueryable();
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            q = q.Where(s => _db.Partners.Any(p => p.Id == s.PartnerId
                && _db.Leads.Any(l => l.Id == p.LeadId && l.OwnerId == uid)));
        }

        return q;
    }

    private IQueryable<Partner> ScopedPartners()
    {
        var q = _db.Partners.AsNoTracking().AsQueryable();
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            q = q.Where(p => _db.Leads.Any(l => l.Id == p.LeadId && l.OwnerId == uid));
        }

        return q;
    }
}
