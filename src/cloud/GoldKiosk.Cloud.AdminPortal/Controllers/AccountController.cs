using System.Security.Claims;
using GoldKiosk.Cloud.AdminPortal.Logging;
using GoldKiosk.Cloud.AdminPortal.Models;
using GoldKiosk.Cloud.AdminPortal.Models.Auth;
using GoldKiosk.Cloud.AdminPortal.Models.Invitations;
using GoldKiosk.Cloud.AdminPortal.Services.Common;
using GoldKiosk.Cloud.AdminPortal.Services.Invitations;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Account controller.</summary>
// Anonymous access is granted per action; a class-level [AllowAnonymous] would
// override [Authorize] on Profile (ASP0026) and expose it unauthenticated.
public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;
    private readonly ILogger<AccountController> _logger;

    /// <summary>Initializes a new instance of the <see cref="AccountController"/> class.</summary>
    public AccountController(
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager,
        AppDbContext db,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _db = db;
        _logger = logger;
    }

    // ───────────────────────── Login ────────────────────────────────────────

    /// <summary>Login.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Login(string? returnUrl = null, string? tab = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirectOrHome(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ActiveTab"] = (tab == "activate") ? "activate" : "signin";
        return View(new AuthPageViewModel());
    }

    /// <summary>Login.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        [Bind(Prefix = "SignIn")] AdminLoginViewModel model,
        string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ActiveTab"] = "signin";

        var page = new AuthPageViewModel { SignIn = model };

        if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(string.Empty, "Email and password are required.");
            return View(nameof(Login), page);
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null || user.DeletedAt is not null || user.Status == "suspended")
        {
            ModelState.AddModelError(string.Empty, "Invalid sign-in attempt.");
            return View(nameof(Login), page);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, isPersistent: model.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            _logger.AccountLockedOut(user.Id);
            ModelState.AddModelError(string.Empty, "This account is locked. Try again later.");
            return View(nameof(Login), page);
        }

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid sign-in attempt.");
            return View(nameof(Login), page);
        }

        await SignInWithDomainClaimsAsync(user, model.RememberMe);

        user.LastSignedInAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        _logger.UserSignedIn(user.Id);
        return LocalRedirectOrHome(returnUrl);
    }

    // ───────────────────────── Activate (signup with key) ────────────────────

    /// <summary>Activate.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [ActionName("Activate")]
    public async Task<IActionResult> Activate(
        [Bind(Prefix = "Activate")] ActivateViewModel model,
        string? returnUrl = null,
        CancellationToken ct = default)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ActiveTab"] = "activate";

        var page = new AuthPageViewModel { Activate = model };

        if (!ModelState.IsValid)
        {
            return View(nameof(Login), page);
        }

        // ─── Open the connection ONCE for this controller action ─────────
        // Every raw-SQL step below reuses this connection. EF still owns its
        // lifetime — we never call Close() — but we do ensure it's open up
        // front so command sequencing is predictable. The connection is the
        // same instance returned by every db.Database.GetDbConnection() call
        // (DbContext holds one per-instance), so handing the cmds the same
        // open connection is a behaviour-preserving refactor.
        var conn = _db.Database.GetDbConnection();
        await conn.EnsureOpenAsync(ct);

        // Validate the key + email via the SECURITY DEFINER function in PG.
        // Returns tenant_id, key_id, owner_role_id, key_prefix on success.
        Guid tenantId, keyId, ownerRoleId;
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM identity.validate_activation_key(@key, @email)";

            var keyParam = cmd.CreateParameter();
            keyParam.ParameterName = "key";
            keyParam.Value = model.ActivationKey.Trim();
            cmd.Parameters.Add(keyParam);

            var emailParam = cmd.CreateParameter();
            emailParam.ParameterName = "email";
            emailParam.Value = model.Email.Trim();
            cmd.Parameters.Add(emailParam);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct))
            {
                ModelState.AddModelError(string.Empty, "Activation key + email did not match our records.");
                return View(nameof(Login), page);
            }

            tenantId = reader.GetGuid(0);
            keyId = reader.GetGuid(1);
            ownerRoleId = reader.GetGuid(2);
        }
        catch (Npgsql.PostgresException pgex) when (pgex.SqlState == "22023")
        {
            // Our function raises 22023 on any validation failure.
            _logger.ActivationKeyValidationFailed(pgex);
            ModelState.AddModelError(string.Empty,
                "Activation key + email did not match our records, or the key has expired / been consumed.");
            return View(nameof(Login), page);
        }
        catch (Npgsql.PostgresException pgex) when (pgex.SqlState == "42883" || pgex.SqlState == "42P01")
        {
            // 42883 = function does not exist; 42P01 = relation/table does not exist.
            // This means the activation-key DB migration (0033) has not been applied.
            _logger.ActivationKeyDbObjectsMissing(pgex);
            ModelState.AddModelError(string.Empty,
                "Activation key service is not available on this instance. Contact the platform administrator.");
            return View(nameof(Login), page);
        }

        // The key was good. Set tenant context to the tenant we discovered, so
        // RLS lets the UserManager INSERT into identity.users below.
        // Use the shared helper — parameterised, no string-interpolated GUID.
        await conn.SetTenantContextAsync(tenantId, ct);

        // Make sure nobody is already using this email under this tenant.
        var existing = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (existing is not null)
        {
            ModelState.AddModelError(string.Empty,
                "A user with that email already exists. Use 'Sign in' instead.");
            return View(nameof(Login), page);
        }

        // Split the full name. Last token is family name; everything before = given.
        var nameTokens = model.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameTokens.Length > 1
            ? string.Join(' ', nameTokens[..^1])
            : nameTokens.FirstOrDefault() ?? "Admin";
        var lastName = nameTokens.Length > 1 ? nameTokens[^1] : "";

        var newUser = new AppUser
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            OtpMode = "off",
            Status = "active",
            Locale = "en-AE",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        var createResult = await _userManager.CreateAsync(newUser, model.Password);
        if (!createResult.Succeeded)
        {
            foreach (var err in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, err.Description);
            }

            return View(nameof(Login), page);
        }

        // Assign the Owner role for this tenant. Parameterised — no string-
        // interpolated GUIDs. Re-grab the connection in case UserManager's
        // SaveChanges closed it (it shouldn't with pooled connections, but
        // EnsureOpen is cheap insurance).
        await conn.EnsureOpenAsync(ct);
        await using (var roleCmd = conn.CreateCommand())
        {
            roleCmd.CommandText = @"
                INSERT INTO identity.user_roles (user_id, role_id, granted_at)
                VALUES (@uid, @rid, now())";
            var uidP = roleCmd.CreateParameter();
            uidP.ParameterName = "uid";
            uidP.Value = newUser.Id;
            var ridP = roleCmd.CreateParameter();
            ridP.ParameterName = "rid";
            ridP.Value = ownerRoleId;
            roleCmd.Parameters.Add(uidP);
            roleCmd.Parameters.Add(ridP);
            await roleCmd.ExecuteNonQueryAsync(ct);
        }

        // Mark the activation key consumed (atomic — guards against races).
        try
        {
            await using var consumeCmd = conn.CreateCommand();
            consumeCmd.CommandText = "SELECT identity.consume_activation_key(@kid, @uid)";

            var kid = consumeCmd.CreateParameter();
            kid.ParameterName = "kid";
            kid.Value = keyId;
            var uid = consumeCmd.CreateParameter();
            uid.ParameterName = "uid";
            uid.Value = newUser.Id;
            consumeCmd.Parameters.Add(kid);
            consumeCmd.Parameters.Add(uid);

            await consumeCmd.ExecuteNonQueryAsync(ct);
        }
        catch (Npgsql.PostgresException pgex) when (pgex.SqlState == "22023")
        {
            // Extremely rare race: another caller consumed the key between
            // validate and consume. Roll the user back.
            _logger.ActivationKeyConsumedConcurrently(pgex, newUser.Id);
            await _userManager.DeleteAsync(newUser);
            ModelState.AddModelError(string.Empty,
                "That activation key was just used by someone else. Please contact your account exec.");
            return View(nameof(Login), page);
        }

        // Sign them in with the same domain claims as the regular flow.
        await SignInWithDomainClaimsAsync(newUser, isPersistent: true);

        _logger.TenantActivated(tenantId, newUser.Id);

        TempData["FlashSuccess"] = $"Welcome to Gold Kiosk, {firstName}. Your tenant is live.";
        return LocalRedirectOrHome(returnUrl);
    }

    // ───────────────────────── Logout ───────────────────────────────────────

    /// <summary>Logout.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        TempData["FlashSuccess"] = "Signed out.";
        return RedirectToAction(nameof(Login));
    }

    // GET fallback so a stray /Account/Logout link still works.
    /// <summary>Logout get.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ActionName("Logout")]
    public async Task<IActionResult> LogoutGet()
    {
        await _signInManager.SignOutAsync();
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return RedirectToAction(nameof(Login));
    }

    // ───────────────────────── Accept invitation ────────────────────────────

    /// <summary>Accept invite.</summary>
    [HttpGet("/invite/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> AcceptInvite(string token,
        [FromServices] IUserInvitationService invitations,
        CancellationToken ct = default)
    {
        var invite = await invitations.ResolveAsync(token ?? "", ct);
        if (invite is null)
        {
            TempData["FlashError"] = "This invitation link is invalid, expired, or has already been used.";
            return RedirectToAction(nameof(Login));
        }

        ViewData["Invite"] = invite;
        return View(new AcceptInviteViewModel
        {
            Token = token!,
            FirstName = invite.FirstName,
            LastName = invite.LastName,
        });
    }

    /// <summary>Accept invite.</summary>
    [HttpPost("/invite/{token}")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptInvite(string token,
        AcceptInviteViewModel model,
        [FromServices] IUserInvitationService invitations,
        CancellationToken ct = default)
    {
        var invite = await invitations.ResolveAsync(token ?? "", ct);
        if (invite is null)
        {
            TempData["FlashError"] = "This invitation link is invalid, expired, or has already been used.";
            return RedirectToAction(nameof(Login));
        }

        ViewData["Invite"] = invite;
        model.Token = token!;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Refuse if a user already exists for this email under this tenant.
        var existing = await _userManager.FindByEmailAsync(invite.InviteeEmail);
        if (existing is not null)
        {
            TempData["FlashError"] = $"An account already exists for {invite.InviteeEmail}. Sign in instead.";
            return RedirectToAction(nameof(Login));
        }

        var conn = _db.Database.GetDbConnection();
        var opened = await conn.EnsureOpenAsync(ct);
        try
        {
            // Set tenant context to the invite's tenant so the AppUser INSERT
            // satisfies RLS on identity.users.
            await conn.SetTenantContextAsync(invite.TenantId, ct);

            var newUser = new AppUser
            {
                Id = Guid.NewGuid(),
                TenantId = invite.TenantId,
                UserName = invite.InviteeEmail,
                Email = invite.InviteeEmail,
                EmailConfirmed = true,
                FirstName = model.FirstName,
                LastName = model.LastName,
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

            // Assign the invitation's role.
            await conn.EnsureOpenAsync(ct);
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO identity.user_roles (user_id, role_id, granted_at)
                    VALUES (@uid, @rid, now())";
                var uid = cmd.CreateParameter();
                uid.ParameterName = "uid";
                uid.Value = newUser.Id;
                cmd.Parameters.Add(uid);
                var rid = cmd.CreateParameter();
                rid.ParameterName = "rid";
                rid.Value = invite.RoleId;
                cmd.Parameters.Add(rid);
                await cmd.ExecuteNonQueryAsync(ct);
            }

            await invitations.MarkAcceptedAsync(invite.Id, newUser.Id, ct);
            await SignInWithDomainClaimsAsync(newUser, isPersistent: true);

            _logger.InvitationAccepted(invite.Id, newUser.Id, invite.TenantId, invite.RoleCode);

            TempData["FlashSuccess"] = $"Welcome, {model.FirstName}. You're signed in to {invite.TenantLegalName}.";
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

    /// <summary>Denied.</summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Denied() => View();

    /// <summary>Profile.</summary>
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        // Tiny stand-in until a richer profile page lands.
        var u = await _userManager.GetUserAsync(User);
        return View("Profile", u);
    }

    // ───────────────────────── helpers ──────────────────────────────────────

    private async Task SignInWithDomainClaimsAsync(AppUser user, bool isPersistent)
    {
        var extraClaims = new List<Claim>
        {
            new(CurrentUserService.TenantIdClaimType, user.TenantId.ToString()),
            new(ClaimTypes.GivenName, user.FirstName ?? string.Empty),
            new(ClaimTypes.Surname,   user.LastName  ?? string.Empty),
        };
        await _signInManager.SignInWithClaimsAsync(user, isPersistent, extraClaims);
    }

    private IActionResult LocalRedirectOrHome(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}

/// <summary>
/// Composite VM for the dual-tab auth page (sign in + activate). Either the
/// sign-in side or the activate side is the one being submitted; the other
/// is just held empty so model-validation on the active tab doesn't trip
/// the inactive one.
/// </summary>
public sealed class AuthPageViewModel
{
    /// <summary>Gets or sets the sign in.</summary>
    public AdminLoginViewModel SignIn { get; set; } = new();
    /// <summary>Gets or sets the activate.</summary>
    public ActivateViewModel Activate { get; set; } = new();
}
