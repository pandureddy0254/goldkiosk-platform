using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers.Api;

/// <summary>Dashboard KPI JSON endpoints polled by the landing page.</summary>
[ApiController]
[Route("api/dashboard")]
public class DashboardApiController : ControllerBase
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity for rep scoping.</param>
    public DashboardApiController(CrmDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Returns open-lead / active-partner / subscription counts for the caller.</summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("kpis")]
    public async Task<IActionResult> Kpis(CancellationToken ct)
    {
        // List<string> (not string[]) so EF binds Enumerable.Contains -> SQL IN.
        // A string[] binds the new .NET 10 MemoryExtensions.Contains(ReadOnlySpan)
        // overload, which EF cannot translate.
        var openStages = new List<string> { "new", "contacted", "qualified", "proposal" };

        var leadsQuery = _db.Leads.AsNoTracking()
            .Where(l => l.DeletedAt == null && openStages.Contains(l.Stage));
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            leadsQuery = leadsQuery.Where(l => l.OwnerId == uid);
        }

        var openLeads = await leadsQuery.CountAsync(ct);

        var partnersQuery = _db.Partners.AsNoTracking()
            .Where(p => p.TenantStatus == "active");
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid2)
        {
            partnersQuery = partnersQuery.Where(p => _db.Leads.Any(l => l.Id == p.LeadId && l.OwnerId == uid2));
        }

        var activePartners = await partnersQuery.CountAsync(ct);

        var subsStatuses = new List<string> { "trialing", "active", "past_due" };
        var subsQuery = _db.Subscriptions.AsNoTracking()
            .Where(s => subsStatuses.Contains(s.Status));
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid3)
        {
            subsQuery = subsQuery.Where(s => _db.Partners.Any(p => p.Id == s.PartnerId
                && _db.Leads.Any(l => l.Id == p.LeadId && l.OwnerId == uid3)));
        }

        var subscriptions = await subsQuery.CountAsync(ct);

        return Ok(new
        {
            openLeads,
            activePartners,
            subscriptions,
            queuedReminders = 0
        });
    }
}
