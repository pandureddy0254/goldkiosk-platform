using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Infrastructure;
using GoldKiosk.Cloud.CRMPortal.Logging;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>
/// Subscriptions board: filterable list, reminders rail, invoice feed, and cancel action.
/// TODO(GK-CRM-1): the Stripe webhook ingestion and the daily renewal crons the view
/// copy references (02:00 / 02:30 / 03:00) are not ported yet — external_* columns
/// only fill once that ingestion lands.
/// </summary>
public class SubscriptionsController : Controller
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SubscriptionsController> _logger;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity for rep scoping.</param>
    /// <param name="logger">Logger (subscription ids only).</param>
    public SubscriptionsController(CrmDbContext db, ICurrentUserService currentUser, ILogger<SubscriptionsController> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
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

    /// <summary>Lists subscriptions with search/status/cycle/renewal/auto-renew filters.</summary>
    /// <param name="q">Partner-name search.</param>
    /// <param name="status">Status filter or "all".</param>
    /// <param name="cycle">Billing-cycle filter or "all".</param>
    /// <param name="renewal">Renewal window: next_7d | next_30d | next_60d | past_due | any.</param>
    /// <param name="autorenew">"on" | "off" | "any".</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(
        string? q,
        string? status,
        string? cycle,
        string? renewal,
        string? autorenew,
        CancellationToken ct)
    {
        ViewData["ActiveSection"] = "subscriptions";

        ViewBag.Q = q;
        ViewBag.Status = status;
        ViewBag.Cycle = cycle;
        ViewBag.Renewal = renewal;
        ViewBag.Autorenew = autorenew;
        ViewBag.IsManager = _currentUser.IsManager;

        // Pull all visible partners upfront — needed for name-search + table render.
        var allPartnersList = await ScopedPartners().ToListAsync(ct);
        var partners = allPartnersList.ToDictionary(p => p.Id, p => p);

        var subsQuery = ScopedSubscriptions();

        // Partner name search — filter by matching partner IDs.
        if (!string.IsNullOrWhiteSpace(q))
        {
            var matchingIds = allPartnersList
                .Where(p => (p.LegalName?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
                          || (p.DisplayName?.Contains(q, StringComparison.OrdinalIgnoreCase) == true))
                .Select(p => p.Id)
                .ToList();

            if (matchingIds.Count > 0)
            {
                subsQuery = subsQuery.Where(s => matchingIds.Contains(s.PartnerId));
            }
            else
            {
                ViewBag.Partners = partners;
                ViewBag.Reminders = new List<RenewalReminder>();
                ViewBag.Invoices = new List<SubscriptionPeriod>();
                return View(new List<Subscription>());
            }
        }

        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
        {
            subsQuery = subsQuery.Where(s => s.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(cycle) && !string.Equals(cycle, "all", StringComparison.OrdinalIgnoreCase))
        {
            subsQuery = subsQuery.Where(s => s.BillingCycle == cycle);
        }

        // Renewal window filter — translate to a current_period_end ceiling date.
        if (!string.IsNullOrWhiteSpace(renewal) && !string.Equals(renewal, "any", StringComparison.OrdinalIgnoreCase))
        {
            if (renewal == "past_due")
            {
                subsQuery = subsQuery.Where(s => s.Status == "past_due");
            }
            else
            {
                var days = renewal switch
                {
                    "next_7d" => 7,
                    "next_30d" => 30,
                    "next_60d" => 60,
                    _ => 30
                };
                var ceiling = DateTime.UtcNow.AddDays(days);
                subsQuery = subsQuery.Where(s => s.CurrentPeriodEnd <= ceiling);
            }
        }

        if (!string.IsNullOrWhiteSpace(autorenew) && !string.Equals(autorenew, "any", StringComparison.OrdinalIgnoreCase))
        {
            var isOn = string.Equals(autorenew, "on", StringComparison.OrdinalIgnoreCase);
            subsQuery = subsQuery.Where(s => s.AutoRenew == isOn);
        }

        var subs = await subsQuery
            .OrderBy(s => s.CurrentPeriodEnd)
            .Take(100)
            .ToListAsync(ct);

        // Reminders (next 60d window). scheduled_for is a DATE column.
        var horizon60 = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)).ToDateTime(TimeOnly.MinValue);
        var remindersQuery = _db.RenewalReminders.AsNoTracking()
            .Where(r => r.ScheduledFor <= horizon60);
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            remindersQuery = remindersQuery.Where(r => _db.Subscriptions.Any(s => s.Id == r.SubscriptionId
                && _db.Partners.Any(p => p.Id == s.PartnerId
                    && _db.Leads.Any(l => l.Id == p.LeadId && l.OwnerId == uid))));
        }

        var reminders = await remindersQuery
            .OrderBy(r => r.ScheduledFor)
            .Take(10)
            .ToListAsync(ct);

        // Recent invoices for the bottom-right feed.
        var invoicesQuery = _db.SubscriptionPeriods.AsNoTracking().AsQueryable();
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid2)
        {
            invoicesQuery = invoicesQuery.Where(sp => _db.Subscriptions.Any(s => s.Id == sp.SubscriptionId
                && _db.Partners.Any(p => p.Id == s.PartnerId
                    && _db.Leads.Any(l => l.Id == p.LeadId && l.OwnerId == uid2))));
        }

        var invoices = await invoicesQuery
            .OrderByDescending(sp => sp.InvoicedAt)
            .Take(8)
            .ToListAsync(ct);

        ViewBag.Partners = partners;
        ViewBag.Reminders = reminders;
        ViewBag.Invoices = invoices;
        return View(subs);
    }

    /// <summary>Manager-only. Cancels a subscription via the billing.cancel_subscription RPC.</summary>
    /// <param name="id">Subscription id.</param>
    /// <param name="reason">Cancellation reason (recorded).</param>
    /// <param name="effectiveAt">Effective date; NULL = cancel at end of current period.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost]
    [RequireManager]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, string reason, DateTime? effectiveAt, CancellationToken ct)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("missing subscription id");
        }

        try
        {
            // p_effective_at NULL = cancel at end of current period.
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT billing.cancel_subscription({id}, {reason ?? "(no reason given)"}, {effectiveAt})", ct);
            TempData["BillingFlash"] = "Subscription cancelled.";
        }
        catch (Exception ex)
        {
            _logger.SubscriptionCancelFailed(ex, id);
            TempData["BillingFlash"] = "Cancel failed — operations have been notified.";
        }

        return RedirectToAction(nameof(Index));
    }
}
