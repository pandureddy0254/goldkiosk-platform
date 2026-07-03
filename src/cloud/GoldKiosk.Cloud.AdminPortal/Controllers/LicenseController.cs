using GoldKiosk.Cloud.AdminPortal.Logging;
using System.Security.Claims;
using GoldKiosk.Cloud.AdminPortal.Models.Licensing;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Cloud.AdminPortal.Services.Licensing;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>License controller.</summary>
[AllowAnonymous]
[Route("License")]
public sealed class LicenseController : Controller
{
    private static readonly char[] _regionHintSeparators = [' ', '·', '-', '/'];

    private readonly ILicenseService _licenses;
    private readonly AppDbContext _db;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<LicenseController> _logger;

    /// <summary>Initializes a new instance of the <see cref="LicenseController"/> class.</summary>
    public LicenseController(
        ILicenseService licenses,
        AppDbContext db,
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager,
        ILogger<LicenseController> logger)
    {
        _licenses = licenses;
        _db = db;
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    // ────────────── Step 1: paste license token ──────────────

    /// <summary>Activate.</summary>
    [HttpGet("Activate")]
    [HttpGet("/activate")]
    public async Task<IActionResult> Activate(CancellationToken ct)
    {
        var existing = await _licenses.GetActiveAsync(ct);
        if (existing is not null)
        {
            ViewData["ExistingLicense"] = existing.License;
        }

        return View(new LicenseActivationViewModel());
    }

    /// <summary>Activate.</summary>
    [HttpPost("Activate")]
    [HttpPost("/activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(LicenseActivationViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var outcome = await _licenses.ActivateAsync(model.LicenseToken.Trim(), ct);
        if (!outcome.Valid)
        {
            ModelState.AddModelError(nameof(model.LicenseToken), outcome.Error ?? "License token is invalid.");
            return View(model);
        }

        var lic = outcome.License!;

        // If an Owner user already exists for this tenant, hop straight to login —
        // the user just re-pasted their token to refresh the license.
        var ownerEmail = lic.AdminEmail;
        var existingUser = string.IsNullOrWhiteSpace(ownerEmail)
            ? null
            : await _userManager.FindByEmailAsync(ownerEmail);
        if (existingUser is not null && existingUser.TenantId == lic.TenantId)
        {
            TempData["FlashSuccess"] = $"Licence refreshed for {lic.LegalName}. Sign in with {ownerEmail} to continue.";
            return RedirectToAction("Login", "Account");
        }

        // First-run for this licence — collect a password + name and finish provisioning.
        return RedirectToAction(nameof(SetupAccount));
    }

    // ────────────── Step 2: create Owner user + auto sign-in ──────────────

    /// <summary>Setup account.</summary>
    [HttpGet("SetupAccount")]
    public async Task<IActionResult> SetupAccount(CancellationToken ct)
    {
        var stored = await _licenses.GetActiveAsync(ct);
        if (stored is null)
        {
            return RedirectToAction(nameof(Activate));
        }

        ViewData["License"] = stored.License;
        return View(new LicenseSetupAccountViewModel());
    }

    /// <summary>Setup account.</summary>
    [HttpPost("SetupAccount")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetupAccount(LicenseSetupAccountViewModel model, CancellationToken ct)
    {
        var stored = await _licenses.GetActiveAsync(ct);
        if (stored is null)
        {
            return RedirectToAction(nameof(Activate));
        }

        var lic = stored.License;
        ViewData["License"] = lic;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // ─── Re-verify the licence (defence-in-depth — between steps the JWKS
        //     could have rotated or the token been revoked).
        var reverify = _licenses.ReverifyStored(stored);
        if (!reverify.Valid)
        {
            TempData["FlashError"] = reverify.Error ?? "Licence is no longer valid. Re-activate to continue.";
            return RedirectToAction(nameof(Activate));
        }

        if (string.IsNullOrWhiteSpace(lic.AdminEmail))
        {
            ModelState.AddModelError(string.Empty,
                "Licence is missing an admin email — contact partner-success to re-issue.");
            return View(model);
        }

        // If a user with this email already exists, fast-path to sign in.
        var existingUser = await _userManager.FindByEmailAsync(lic.AdminEmail);
        if (existingUser is not null)
        {
            TempData["FlashSuccess"] = $"An account already exists for {lic.AdminEmail}. Sign in to continue.";
            return RedirectToAction("Login", "Account");
        }

        // ─── Provision the tenant (if missing) + roles + Owner user ──────────
        var conn = _db.Database.GetDbConnection();
        var opened = await conn.EnsureOpenAsync(ct);
        try
        {
            await conn.SetTenantContextAsync(lic.TenantId, ct);

            // Insert tenant if it doesn't exist. The license is the source of
            // truth for legal_name + display_name + tier inferred from plan.
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO tenancy.tenants
                        (id, code, legal_name, tier, home_country_code, data_residency_region, status, onboarded_at)
                    VALUES
                        (@id, @code, @legal, @tier, @cc, @region, 'active', now())
                    ON CONFLICT (id) DO NOTHING";
                AddParam(cmd, "id", lic.TenantId);
                AddParam(cmd, "code", DeriveTenantCode(lic));
                AddParam(cmd, "legal", string.IsNullOrWhiteSpace(lic.LegalName) ? lic.DisplayName : lic.LegalName);
                AddParam(cmd, "tier", DeriveTier(lic.PlanCode));
                AddParam(cmd, "cc", DeriveCountryCode(lic.RegionHint));
                AddParam(cmd, "region", DeriveAzureRegion(lic.RegionHint));
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // Apply system RBAC roles to this tenant (idempotent).
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT identity.apply_system_roles(@tid)";
                AddParam(cmd, "tid", lic.TenantId);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            // ─── Create the Owner AppUser via UserManager ────────────────────
            var nameTokens = model.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var firstName = nameTokens.Length > 1
                ? string.Join(' ', nameTokens[..^1])
                : nameTokens.FirstOrDefault() ?? "Admin";
            var lastName = nameTokens.Length > 1 ? nameTokens[^1] : "";

            var newUser = new AppUser
            {
                Id = Guid.NewGuid(),
                TenantId = lic.TenantId,
                UserName = lic.AdminEmail,
                Email = lic.AdminEmail,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                OtpMode = "off",
                Status = "active",
                Locale = "en-AE",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            };

            var create = await _userManager.CreateAsync(newUser, model.Password);
            if (!create.Succeeded)
            {
                foreach (var err in create.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }

                return View(model);
            }

            // Assign Owner role for this tenant.
            await conn.EnsureOpenAsync(ct);
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO identity.user_roles (user_id, role_id, granted_at)
                    SELECT @uid, r.id, now()
                      FROM identity.roles r
                     WHERE r.tenant_id = @tid
                       AND r.code      = 'owner'
                       AND r.is_active = true
                       AND NOT EXISTS (
                           SELECT 1
                             FROM identity.user_roles ur
                            WHERE ur.user_id = @uid
                              AND ur.role_id = r.id
                              AND ur.revoked_at IS NULL
                       )";
                AddParam(cmd, "uid", newUser.Id);
                AddParam(cmd, "tid", lic.TenantId);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            _logger.OwnerProvisionedFromLicense(lic.TenantId, lic.LegalName);

            // ─── Sign in with full domain claims and bounce to the dashboard ──
            await SignInWithDomainClaimsAsync(newUser);

            TempData["FlashSuccess"] = $"Welcome to Gold Kiosk, {firstName}. {lic.LegalName} is live.";
            return RedirectToAction("Index", "Home");
        }
        finally
        {
            if (opened)
            {
                await conn.CloseAsync();
            }
        }
    }

    // ────────────── helpers ──────────────

    private async Task SignInWithDomainClaimsAsync(AppUser user)
    {
        var extraClaims = new List<Claim>
        {
            new(CurrentUserService.TenantIdClaimType, user.TenantId.ToString()),
            new(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
            new(ClaimTypes.Surname,   user.LastName  ?? string.Empty),
        };
        await _signInManager.SignInWithClaimsAsync(user, isPersistent: true, extraClaims);
    }

    private static void AddParam(System.Data.Common.DbCommand cmd, string name, object value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        cmd.Parameters.Add(p);
    }

    private static string DeriveTenantCode(License lic)
    {
        // Build a 3-12 char uppercase code from DisplayName (fallback LegalName).
        var src = string.IsNullOrWhiteSpace(lic.DisplayName) ? lic.LegalName : lic.DisplayName;
        var alpha = new string(src.Where(char.IsLetterOrDigit).Take(12).ToArray()).ToUpperInvariant();
        if (alpha.Length < 3)
        {
            alpha = ("T" + lic.TenantId.ToString("N")[..7]).ToUpperInvariant();
        }

        return alpha;
    }

    private static string DeriveTier(string planCode) => planCode?.ToLowerInvariant() switch
    {
        "enterprise" => "bank",
        "bank" => "bank",
        "premium" => "premium",
        _ => "standard",
    };

    private static string DeriveCountryCode(string regionHint)
    {
        if (string.IsNullOrWhiteSpace(regionHint))
        {
            return "AE";
        }
        // region_hint examples: "UAE · DXB", "SG", "IN · BLR".
        var head = regionHint.Split(_regionHintSeparators, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? "";
        return head.ToUpperInvariant() switch
        {
            "UAE" or "AE" => "AE",
            "SG" => "SG",
            "IN" => "IN",
            "GB" or "UK" => "GB",
            "CA" => "CA",
            "US" or "USA" => "US",
            "TT" => "TT",
            _ => "AE",
        };
    }

    private static string DeriveAzureRegion(string regionHint) =>
        DeriveCountryCode(regionHint) switch
        {
            "AE" => "uaenorth",
            "SG" => "southeastasia",
            "IN" => "centralindia",
            "GB" => "uksouth",
            "CA" => "canadacentral",
            "US" => "eastus",
            "TT" => "canadacentral",
            _ => "uaenorth",
        };
}
