using System.Text.Json;
using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models.Integration;
using GoldKiosk.Cloud.AdminPortal.Services;
using GoldKiosk.Infrastructure.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>
/// Partner API credentials catalogue.
/// Permission gate ([Permission("api_credentials:*")]) will be layered in Phase 5;
/// for now <see cref="AuthorizeAttribute"/> + the global fallback policy enforce
/// "must be signed in".
/// </summary>
[Authorize]
public sealed class ApiCredentialsController(
    IPartnerApiCredentialService credentialService,
    ICurrentUserService currentUser,
    ILogger<ApiCredentialsController> logger) : Controller
{
    private const string JustCreatedKey = "ApiCredentials.JustCreated";

    // ───────────────────────────────── Index ────────────────────────────────

    /// <summary>Index.</summary>
    [HttpGet]
    [Permission("api_credentials:read")]
    public async Task<IActionResult> Index(string? env, bool revoked = false, CancellationToken ct = default)
    {
        var environmentFilter = NormaliseEnvFilter(env);
        var environmentForList = environmentFilter == "all" ? null : environmentFilter;

        var rows = await credentialService.ListAsync(environmentForList, includeRevoked: revoked, ct);
        var kpis = await credentialService.GetKpisAsync(ct);

        // ─── One-time-reveal: if the previous request just created a key, surface it ──
        string? cleartextKey = null;
        string? cleartextAppId = null;
        string? justCreatedLabel = null;
        string? justCreatedEnv = null;
        IReadOnlyList<string> justCreatedScopes = Array.Empty<string>();

        if (TempData[JustCreatedKey] is string flashJson)
        {
            try
            {
                var flash = JsonSerializer.Deserialize<JustCreatedCredentialFlash>(flashJson);
                if (flash is not null)
                {
                    cleartextKey = flash.AppKey;
                    cleartextAppId = flash.AppId;
                    justCreatedLabel = flash.Label;
                    justCreatedEnv = flash.Environment;
                    justCreatedScopes = flash.Scopes;
                }
            }
            catch (Exception ex)
            {
                logger.CredentialFlashDeserializeFailed(ex);
            }
        }

        var vm = new PartnerApiCredentialIndexViewModel
        {
            Kpis = kpis,
            Rows = rows,
            EnvironmentFilter = environmentFilter,
            IncludeRevoked = revoked,
            ClearTextAppKey = cleartextKey,
            ClearTextAppId = cleartextAppId,
            JustCreatedLabel = justCreatedLabel,
            JustCreatedEnvironment = justCreatedEnv,
            JustCreatedScopes = justCreatedScopes,
        };

        ViewBag.ScopesCatalogue = PartnerApiCredentialScopes.All;
        return View(vm);
    }

    // ───────────────────────────────── Create ───────────────────────────────

    /// <summary>Create.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("api_credentials:write")]
    public async Task<IActionResult> Create(PartnerApiCredentialCreateRequest req, CancellationToken ct = default)
    {
        if (currentUser.UserId is not Guid actorUserId)
        {
            TempData["FlashError"] = "Sign-in required to generate credentials.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            var summary = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            TempData["FlashError"] = string.IsNullOrWhiteSpace(summary)
                ? "Invalid request — check the form fields."
                : summary;
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var result = await credentialService.CreateAsync(req, actorUserId, ct);

            // Stash the cleartext in TempData (in-memory provider by default —
            // signed cookie if configured). NEVER hits a column.
            var flash = new JustCreatedCredentialFlash
            {
                AppId = result.ClearTextAppId,
                AppKey = result.ClearTextAppKey,
                Label = result.Row.Label,
                Environment = result.Row.Environment,
                Scopes = result.Row.Scopes.ToList(),
            };
            TempData[JustCreatedKey] = JsonSerializer.Serialize(flash);

            TempData["FlashSuccess"] = $"Credential '{result.Row.Label}' generated. Copy the secret now — it will not be shown again.";
            return RedirectToAction(nameof(Index));
        }
        catch (ArgumentException ex)
        {
            logger.CredentialCreateRequestInvalid(ex);
            TempData["FlashError"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            logger.CredentialGenerateFailed(ex);
            TempData["FlashError"] = "Failed to generate credential. See server logs.";
            return RedirectToAction(nameof(Index));
        }
    }

    // ───────────────────────────────── Revoke ───────────────────────────────

    /// <summary>Revoke.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("api_credentials:admin")]
    public async Task<IActionResult> Revoke(Guid id, string? reason, CancellationToken ct = default)
    {
        if (currentUser.UserId is not Guid actorUserId)
        {
            TempData["FlashError"] = "Sign-in required to revoke credentials.";
            return RedirectToAction(nameof(Index));
        }

        if (id == Guid.Empty)
        {
            TempData["FlashError"] = "Credential id missing.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await credentialService.RevokeAsync(id, reason ?? "", actorUserId, ct);
            TempData["FlashSuccess"] = "Credential revoked.";
        }
        catch (Exception ex)
        {
            logger.CredentialRevokeFailed(ex, id);
            TempData["FlashError"] = "Failed to revoke credential. See server logs.";
        }

        return RedirectToAction(nameof(Index));
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private static string NormaliseEnvFilter(string? env)
    {
        if (string.IsNullOrWhiteSpace(env))
        {
            return "all";
        }

        env = env.Trim().ToLowerInvariant();
        return env is "live" or "test" ? env : "all";
    }
}
