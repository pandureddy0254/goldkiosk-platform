using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>
/// Invoice list over billing.subscription_periods.
/// TODO(GK-CRM-1): the Stripe invoice-ingestion webhook that populates
/// external_invoice_id/external_payment_id (referenced by the view footer copy)
/// is not ported yet — rows currently only arrive via the billing RPCs/seed.
/// </summary>
public class InvoicesController : Controller
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity for rep scoping.</param>
    public InvoicesController(CrmDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Lists the latest invoices (subscription periods) with a status filter.</summary>
    /// <param name="status">Status filter or "all".</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(string? status, CancellationToken ct)
    {
        ViewData["ActiveSection"] = "invoices";

        var query = _db.SubscriptionPeriods.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            query = query.Where(p => p.Status == status);
        }

        // Rep scoping: a rep sees only periods for subscriptions on partners
        // descending from leads they own.
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            query = query.Where(sp => _db.Subscriptions.Any(s => s.Id == sp.SubscriptionId
                && _db.Partners.Any(p => p.Id == s.PartnerId
                    && _db.Leads.Any(l => l.Id == p.LeadId && l.OwnerId == uid))));
        }

        var invoices = await query
            .OrderByDescending(p => p.InvoicedAt)
            .Take(50)
            .ToListAsync(ct);

        // Join up partners via subscriptions
        var subById = await ScopedSubscriptions()
            .ToDictionaryAsync(s => s.Id, s => s, ct);

        var partners = await ScopedPartners()
            .ToDictionaryAsync(p => p.Id, p => p, ct);

        ViewBag.SubById = subById;
        ViewBag.Partners = partners;
        ViewBag.Status = status ?? "all";
        return View(invoices);
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
