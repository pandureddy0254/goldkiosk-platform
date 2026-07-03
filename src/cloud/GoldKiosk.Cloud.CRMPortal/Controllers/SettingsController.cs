using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Logging;
using GoldKiosk.Cloud.CRMPortal.Models.ViewModels;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>Workspace settings: profile card + change-password.</summary>
public class SettingsController : Controller
{
    private readonly IProfileService _profiles;
    private readonly ICurrentUserService _currentUser;
    private readonly CrmDbContext _db;
    private readonly ILogger<SettingsController> _logger;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="profiles">Signed-in profile loader.</param>
    /// <param name="currentUser">Per-request user identity.</param>
    /// <param name="db">CRM database context.</param>
    /// <param name="logger">Logger (user ids only — never emails or passwords).</param>
    public SettingsController(
        IProfileService profiles,
        ICurrentUserService currentUser,
        CrmDbContext db,
        ILogger<SettingsController> logger)
    {
        _profiles = profiles;
        _currentUser = currentUser;
        _db = db;
        _logger = logger;
    }

    /// <summary>Renders the settings page.</summary>
    /// <param name="ct">Cancellation token.</param>
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Workspace settings";
        ViewData["ActiveSection"] = "workspace-settings";

        var me = await _profiles.GetCurrentAsync(ct);
        ViewData["Profile"] = me;
        return View(new ChangePasswordViewModel());
    }

    /// <summary>
    /// Changes the password: verifies the current password via crm.verify_login (bcrypt
    /// comparison in the DB), then rotates the hash via pgcrypto. Re-verifying the
    /// current password means a stolen session cookie can't silently rotate it.
    /// </summary>
    /// <param name="vm">Posted change-password form.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(vm);

        ViewData["Title"] = "Workspace settings";
        ViewData["ActiveSection"] = "workspace-settings";

        var me = await _profiles.GetCurrentAsync(ct);
        ViewData["Profile"] = me;

        if (!ModelState.IsValid)
        {
            return View(nameof(Index), vm);
        }

        var email = _currentUser.Email ?? me?.Email;
        if (string.IsNullOrWhiteSpace(email) || _currentUser.UserId is not Guid uid)
        {
            ModelState.AddModelError(string.Empty, "Could not resolve the current session. Please sign in again.");
            return View(nameof(Index), vm);
        }

        try
        {
            // 1. Re-authenticate with the current password (0 or 1 row).
            var matches = await _db.Database
                .SqlQueryRaw<Guid>(
                    "SELECT id AS \"Value\" FROM crm.verify_login({0}, {1})",
                    email, vm.CurrentPassword)
                .ToListAsync(ct);

            if (matches.FirstOrDefault() != uid)
            {
                ModelState.AddModelError(nameof(vm.CurrentPassword), "Current password is incorrect.");
                return View(nameof(Index), vm);
            }

            // 2. Rotate the hash (bcrypt via pgcrypto), matching the seed scheme.
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE crm.profiles SET password_hash = public.crypt({vm.NewPassword}, public.gen_salt('bf')), updated_at = now() WHERE id = {uid}",
                ct);
        }
        catch (Exception ex)
        {
            _logger.PasswordChangeFailed(ex, uid);
            ModelState.AddModelError(string.Empty, "Password update failed. Please try again.");
            return View(nameof(Index), vm);
        }

        TempData["SettingsFlash"] = "Password updated. Use it the next time you sign in.";
        return RedirectToAction(nameof(Index));
    }
}
