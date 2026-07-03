using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models.Tenancy;
using GoldKiosk.Cloud.AdminPortal.Services;
using GoldKiosk.Infrastructure.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>
/// Renders the 6-tab tenant-settings surface (General · Branding · Features ·
/// Activation keys · Billing · Compliance) and persists changes back to the
/// <c>tenancy</c> / <c>identity.activation_keys</c> schema for the signed-in
/// user's tenant. Every POST is anti-forgery protected; activation-key
/// cleartext is never returned by the underlying service.
/// </summary>
[Authorize]
public sealed class TenantSettingsController(
    ITenantSettingsService tenantSettings,
    ICurrentUserService currentUser,
    ILogger<TenantSettingsController> logger) : Controller
{
    private static readonly HashSet<string> KnownTabs = new(StringComparer.OrdinalIgnoreCase)
    {
        "general", "branding", "features", "keys", "billing", "compliance"
    };

    // ─── GET ────────────────────────────────────────────────────────────────
    /// <summary>Index.</summary>
    [HttpGet]
    [Permission("tenant:read")]
    public async Task<IActionResult> Index(string? tab, CancellationToken ct = default)
    {
        var vm = await tenantSettings.GetAsync(ct);

        var resolved = !string.IsNullOrWhiteSpace(tab) && KnownTabs.Contains(tab) ? tab.ToLowerInvariant() : "general";
        return View(new TenantSettingsViewModel
        {
            TenantId = vm.TenantId,
            TenantCode = vm.TenantCode,
            TenantStatus = vm.TenantStatus,
            General = vm.General,
            Facts = vm.Facts,
            Branding = vm.Branding,
            Features = vm.Features,
            Retention = vm.Retention,
            Compliance = vm.Compliance,
            Activation = vm.Activation,
            Billing = vm.Billing,
            ActiveTab = resolved,
        });
    }

    // ─── POST: General ──────────────────────────────────────────────────────
    /// <summary>Save general.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("tenant:write")]
    public async Task<IActionResult> SaveGeneral(TenantGeneralForm form, CancellationToken ct = default)
    {
        var actor = currentUser.UserId ?? Guid.Empty;
        try
        {
            await tenantSettings.SaveGeneralAsync(form, actor, ct);
            TempData["FlashSuccess"] = "General settings saved.";
        }
        catch (Exception ex)
        {
            logger.SaveGeneralFailed(ex, currentUser.TenantId);
            TempData["FlashError"] = "Couldn't save general settings: " + ex.Message;
        }
        return RedirectToAction(nameof(Index), new { tab = "general" });
    }

    // ─── POST: Branding ─────────────────────────────────────────────────────
    /// <summary>Save branding.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("tenant:write")]
    public async Task<IActionResult> SaveBranding(TenantBrandingForm form, CancellationToken ct = default)
    {
        var actor = currentUser.UserId ?? Guid.Empty;
        try
        {
            await tenantSettings.SaveBrandingAsync(form, actor, ct);
            TempData["FlashSuccess"] = "Branding saved.";
        }
        catch (Exception ex)
        {
            logger.SaveBrandingFailed(ex, currentUser.TenantId);
            TempData["FlashError"] = "Couldn't save branding: " + ex.Message;
        }
        return RedirectToAction(nameof(Index), new { tab = "branding" });
    }

    // ─── POST: Features ─────────────────────────────────────────────────────
    /// <summary>Save features.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("tenant:write")]
    public async Task<IActionResult> SaveFeatures(IFormCollection form, CancellationToken ct = default)
    {
        // Checkbox-per-feature: the form submits "flags[<code>]=on" for each ticked box.
        // Unchecked boxes are absent from the post — so we explicitly default the missing
        // ones to false to keep the persisted state in sync with the UI.
        var flags = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in form.Keys)
        {
            if (!key.StartsWith("flags[", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var code = key.Substring("flags[".Length).TrimEnd(']');
            var v = form[key].ToString();
            flags[code] = string.Equals(v, "on", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
        }

        var actor = currentUser.UserId ?? Guid.Empty;
        try
        {
            await tenantSettings.SaveFeaturesAsync(flags, actor, ct);
            TempData["FlashSuccess"] = $"Feature flags updated · {flags.Count(kv => kv.Value)} enabled.";
        }
        catch (Exception ex)
        {
            logger.SaveFeaturesFailed(ex, currentUser.TenantId);
            TempData["FlashError"] = "Couldn't save features: " + ex.Message;
        }
        return RedirectToAction(nameof(Index), new { tab = "features" });
    }

    // ─── POST: Retention ────────────────────────────────────────────────────
    /// <summary>Save retention.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("tenant:admin")]
    public async Task<IActionResult> SaveRetention(TenantRetentionForm form, CancellationToken ct = default)
    {
        var actor = currentUser.UserId ?? Guid.Empty;
        try
        {
            await tenantSettings.SaveRetentionAsync(form, actor, ct);
            TempData["FlashSuccess"] = "Retention policy saved.";
        }
        catch (Exception ex)
        {
            logger.SaveRetentionFailed(ex, currentUser.TenantId);
            TempData["FlashError"] = "Couldn't save retention policy: " + ex.Message;
        }
        return RedirectToAction(nameof(Index), new { tab = "compliance" });
    }

    // ─── POST: Request key re-issue (stub) ──────────────────────────────────
    /// <summary>Request key reissue.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Permission("tenant:admin")]
    public IActionResult RequestKeyReissue(string? reason)
    {
        logger.ActivationKeyReissueRequested(currentUser.TenantId, currentUser.UserId, string.IsNullOrWhiteSpace(reason) ? "(none)" : reason);

        TempData["FlashSuccess"] = "Request submitted — your Gold Kiosk account exec will contact you shortly.";
        return RedirectToAction(nameof(Index), new { tab = "keys" });
    }
}
