using GoldKiosk.Cloud.CRMPortal.Models.ViewModels;
using GoldKiosk.Cloud.CRMPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.CRMPortal.Controllers;

/// <summary>Sign-in / sign-out / access-denied pages (anonymous by design).</summary>
[AllowAnonymous]
public class AccountController : Controller
{
    private readonly ICrmAuthService _auth;

    /// <summary>Initializes the controller with the authentication service.</summary>
    /// <param name="auth">Cookie authentication service.</param>
    public AccountController(ICrmAuthService auth) => _auth = auth;

    /// <summary>Renders the sign-in form.</summary>
    /// <param name="returnUrl">Optional local URL to return to after sign-in.</param>
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["Title"] = "Sign in";
        // Drop non-local URLs immediately so we never reflect an attacker-controlled
        // value into the hidden ReturnUrl input. asp-for prefers ModelState over the
        // Model, so we must also clear the auto-populated query-string entry.
        if (!string.IsNullOrEmpty(returnUrl) && !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = null;
        }

        ModelState.Remove(nameof(LoginViewModel.ReturnUrl));
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    /// <summary>Handles the sign-in form post.</summary>
    /// <param name="vm">Posted credentials.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(vm);

        ViewData["Title"] = "Sign in";
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var result = await _auth.SignInAsync(HttpContext, vm.Email, vm.Password, ct);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Sign-in failed");
            return View(vm);
        }

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
        {
            return Redirect(vm.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Graceful GET handler: stray GETs (bookmarks, prefetch, poisoned returnUrl) redirect
    /// to Login without signing out — prevents HTTP 405 from the POST-only overload below.
    /// </summary>
    [HttpGet]
    public new IActionResult SignOut() => RedirectToAction(nameof(Login));

    /// <summary>Signs the user out and returns to the login page.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("SignOut")]
    public async Task<IActionResult> SignOutPost()
    {
        await _auth.SignOutAsync(HttpContext);
        return RedirectToAction(nameof(Login));
    }

    /// <summary>Cookie middleware redirects 403s here (AccessDeniedPath = /Account/Denied).</summary>
    [HttpGet]
    public IActionResult Denied()
    {
        ViewData["Title"] = "Access denied";
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("~/Views/Shared/Error.cshtml", new Models.ErrorViewModel
        {
            RequestId = HttpContext.TraceIdentifier
        });
    }
}
