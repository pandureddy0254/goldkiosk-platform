using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers.Api;

/// <summary>
/// Billing KPI JSON endpoints for the subscriptions board.
/// TODO(GK-CRM-1): figures come from billing.subscriptions rows only; the Stripe
/// ingestion feed that keeps them current in production is not ported yet.
/// </summary>
[ApiController]
[Route("api/billing")]
public class BillingApiController : ControllerBase
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity for rep scoping.</param>
    public BillingApiController(CrmDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Returns renewal/MRR/ARR/past-due KPIs for the caller's visible subscriptions.</summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis(CancellationToken ct)
    {
        var query = _db.Subscriptions.AsNoTracking().AsQueryable();
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            query = query.Where(s => _db.Partners.Any(p => p.Id == s.PartnerId
                && _db.Leads.Any(l => l.Id == p.LeadId && l.OwnerId == uid)));
        }

        var rows = await query.ToListAsync(ct);
        var now = DateTime.UtcNow;
        var thirtyOut = now.AddDays(30);
        var yearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var renewals30 = rows.Count(r =>
            (r.Status == "active" || r.Status == "trialing")
            && r.CurrentPeriodEnd >= now && r.CurrentPeriodEnd <= thirtyOut);

        var activeSubs = rows.Where(r => r.Status == "active" || r.Status == "trialing").ToList();
        var pastDue = rows.Count(r => r.Status == "past_due");
        var arrYtdRows = rows.Where(r => (r.Status == "active" || r.Status == "trialing") && r.StartedAt <= now && r.StartedAt >= yearStart).ToList();

        var distinctCurrencies = activeSubs.Select(s => s.CurrencyCode).Distinct().ToList();
        decimal mrrTotal = activeSubs.Sum(s => s.MrrAmount);
        decimal arrTotal = arrYtdRows.Sum(s => s.AnnualAmount);
        if (arrTotal == 0)
        {
            arrTotal = activeSubs.Sum(s => s.AnnualAmount);
        }

        string mrrCurrency = distinctCurrencies.Count == 1 ? distinctCurrencies[0] : "mixed";
        string arrCurrency = mrrCurrency;

        return Ok(new
        {
            renewalsNext30 = renewals30,
            activeMrr = Math.Round(mrrTotal, 0),
            mrrCurrency,
            pastDue,
            arrYtd = Math.Round(arrTotal, 0),
            arrCurrency
        });
    }
}
