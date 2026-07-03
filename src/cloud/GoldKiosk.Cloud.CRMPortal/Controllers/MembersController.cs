using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Infrastructure;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>
/// Superadmin-only member management. New CRM logins are created via the
/// crm.create_member SECURITY DEFINER RPC (which bcrypt-hashes the password in
/// the DB and writes an audit row); the superadmin gate is enforced both here
/// ([RequireSuperadmin]) and inside the RPC (crm.is_superadmin()).
/// </summary>
[RequireSuperadmin]
public class MembersController : Controller
{
    private readonly CrmDbContext _db;

    /// <summary>Initializes the controller with the CRM database context.</summary>
    /// <param name="db">CRM database context.</param>
    public MembersController(CrmDbContext db) => _db = db;

    /// <summary>Lists every staff profile.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Members";
        ViewData["ActiveSection"] = "members";

        var members = await _db.Profiles.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

        return View(members);
    }

    /// <summary>Creates a new staff login via the crm.create_member RPC.</summary>
    /// <param name="fullName">Member's full name (required).</param>
    /// <param name="email">Sign-in email (required, unique).</param>
    /// <param name="role">sales_manager or sales_rep only.</param>
    /// <param name="password">Initial password (min 8; hashed in the DB).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string fullName, string email, string role, string password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(password))
        {
            TempData["MemberError"] = "Full name, email, role, and password are all required.";
            return RedirectToAction(nameof(Index));
        }

        // Members are sales staff only — the RPC also enforces this.
        if (role is not (CrmRole.SalesManager or CrmRole.SalesRep))
        {
            TempData["MemberError"] = "Role must be sales_manager or sales_rep.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // p_avatar_url passed as NULL.
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT crm.create_member({fullName.Trim()}, {email.Trim()}, {role}, {password}, {(string?)null})", ct);
            TempData["MemberFlash"] = $"Member '{fullName.Trim()}' created.";
        }
        catch (Exception)
        {
            // create_member raises on duplicate email / weak password / non-superadmin.
            // The generic flash keeps DB error detail (and the email) off the page and out of logs.
            TempData["MemberError"] = "Could not create member. The email may already be in use, or the password is too short (min 8).";
        }

        return RedirectToAction(nameof(Index));
    }
}
