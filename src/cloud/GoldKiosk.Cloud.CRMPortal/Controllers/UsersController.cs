using GoldKiosk.Cloud.CRMPortal.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>Staff users list (RLS decides visibility).</summary>
public class UsersController : Controller
{
    private readonly CrmDbContext _db;

    /// <summary>Initializes the controller with the CRM database context.</summary>
    /// <param name="db">CRM database context.</param>
    public UsersController(CrmDbContext db) => _db = db;

    /// <summary>Lists the profiles the caller may see.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Users";
        ViewData["ActiveSection"] = "users";

        // RLS: profiles_app_select lets a rep see only their own row; a manager
        // (or superadmin) sees every profile. The DB enforces this — the query
        // simply reads what the current role is allowed to.
        var users = await _db.Profiles
            .AsNoTracking()
            .OrderBy(p => p.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        return View(users);
    }
}
