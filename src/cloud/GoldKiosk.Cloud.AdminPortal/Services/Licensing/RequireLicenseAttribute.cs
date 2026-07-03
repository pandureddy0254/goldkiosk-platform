using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>
/// Gate an action (or whole controller) on the active license. Distinct from
/// <c>[Authorize]</c> — a request can be authenticated but unlicensed (or the
/// reverse, during /License/Activate before the user even exists).
///
/// Examples:
/// <code>
/// [RequireLicense]                                  // any valid license
/// [RequireLicense(Feature = "white_label")]         // requires the feature flag
/// [RequireLicense(KioskCapAtLeast = 5)]             // requires a cap of >= 5
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RequireLicenseAttribute : Attribute, IAsyncAuthorizationFilter
{
    /// <summary>Gets or sets the feature.</summary>
    public string? Feature { get; set; }
    /// <summary>Gets or sets the kiosk cap at least.</summary>
    public int KioskCapAtLeast { get; set; }

    /// <summary>Initializes a new instance of the <see cref="RequireLicenseAttribute"/> class.</summary>
    public RequireLicenseAttribute() { }
    /// <summary>Initializes a new instance of the <see cref="RequireLicenseAttribute"/> class.</summary>
    public RequireLicenseAttribute(string feature) { Feature = feature; }

    /// <summary>On authorization.</summary>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var svc = context.HttpContext.RequestServices.GetRequiredService<ILicenseService>();
        var stored = await svc.GetActiveAsync(context.HttpContext.RequestAborted);
        if (stored is null)
        {
            context.Result = new RedirectToActionResult("Activate", "License", null);
            return;
        }

        var outcome = svc.ReverifyStored(stored);
        if (!outcome.Valid)
        {
            context.HttpContext.Items["LicenseError"] = outcome.Error;
            context.Result = new RedirectToActionResult("Activate", "License", null);
            return;
        }

        if (!string.IsNullOrWhiteSpace(Feature) && !outcome.License!.HasFeature(Feature))
        {
            context.Result = new ForbidResult();
            return;
        }

        if (KioskCapAtLeast > 0 && outcome.License!.KioskCap < KioskCapAtLeast)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}
