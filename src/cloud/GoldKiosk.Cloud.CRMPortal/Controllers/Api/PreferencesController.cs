using GoldKiosk.Cloud.CRMPortal.Data;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.CRMPortal.Controllers.Api;

/// <summary>Per-user UI preference endpoints (theme toggle).</summary>
[ApiController]
[Route("api/preferences")]
public class PreferencesController : ControllerBase
{
    private readonly CrmDbContext _db;
    private readonly ICurrentUserService _currentUser;

    /// <summary>Initializes the controller with its collaborators.</summary>
    /// <param name="db">CRM database context.</param>
    /// <param name="currentUser">Per-request user identity.</param>
    public PreferencesController(CrmDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    /// <summary>Theme payload posted by theme.js.</summary>
    /// <param name="Theme">"light" or "dark".</param>
    public record ThemeDto(string Theme);

    /// <summary>
    /// Persists the user's chosen theme to crm.user_preferences. Called
    /// fire-and-forget from theme.js after each toggle.
    /// <para>
    /// [IgnoreAntiforgeryToken] is safe: the request is gated by the
    /// authenticated-user fallback policy, same-origin (SameSite=Lax), and the
    /// JSON body can't be sent cross-origin with credentials without a CORS
    /// preflight.
    /// </para>
    /// </summary>
    /// <param name="dto">The requested theme.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("theme")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SetTheme([FromBody] ThemeDto dto, CancellationToken ct)
    {
        if (dto is null || (dto.Theme != "light" && dto.Theme != "dark"))
        {
            return BadRequest("theme must be 'light' or 'dark'");
        }

        if (_currentUser.UserId is not Guid uid)
        {
            return Unauthorized();
        }

        // Degrade gracefully: a transient DB error must not 500 — the client's
        // localStorage still holds the toggle for the session.
        try
        {
            var existing = await _db.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == uid, ct);

            if (existing is not null)
            {
                existing.Theme = dto.Theme;
            }
            else
            {
                _db.UserPreferences.Add(new UserPreference { UserId = uid, Theme = dto.Theme });
            }

            await _db.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            return Ok(new { theme = dto.Theme, persisted = false });
        }

        return Ok(new { theme = dto.Theme, persisted = true });
    }
}
