using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>API credentials overview page (partner-level placeholder).</summary>
public class ApiCredentialsController : Controller
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity for rep scoping.</param>
    public ApiCredentialsController(CrmDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Lists the newest partners for the credentials table.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "API credentials";
        ViewData["ActiveSection"] = "api-credentials";

        var query = ScopedPartners();
        var partners = await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return View(partners);
    }

    // Managers see all partners; a rep sees only partners descending from leads
    // they own (mirrors partners_app_select).
    private IQueryable<Models.Domain.Partner> ScopedPartners()
    {
        var q = _db.Partners.AsNoTracking().AsQueryable();
        if (!_currentUser.IsManager && _currentUser.UserId is Guid uid)
        {
            q = q.Where(p => _db.Leads.Any(l => l.Id == p.LeadId && l.OwnerId == uid));
        }

        return q;
    }
}
