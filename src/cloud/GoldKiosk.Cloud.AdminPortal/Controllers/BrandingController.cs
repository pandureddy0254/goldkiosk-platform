using GoldKiosk.Cloud.AdminPortal.Services.Licensing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.AdminPortal.Controllers;

/// <summary>Branding controller.</summary>
[AllowAnonymous]
[Route("branding")]
public sealed class BrandingController : Controller
{
    private readonly IBrandingService _branding;
    private readonly ILicenseService _licenses;

    /// <summary>Initializes a new instance of the <see cref="BrandingController"/> class.</summary>
    public BrandingController(IBrandingService branding, ILicenseService licenses)
    {
        _branding = branding;
        _licenses = licenses;
    }

    /// <summary>Css.</summary>
    [HttpGet("{tenantId:guid}/brand.css")]
    public async Task<IActionResult> Css(Guid tenantId, CancellationToken ct)
    {
        if (!await IsAuthorizedAsync(tenantId, ct))
        {
            return NotFound();
        }

        var path = _branding.GetBrandCssPath(tenantId);
        if (path is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public, max-age=300";
        return PhysicalFile(path, "text/css");
    }

    /// <summary>Logo.</summary>
    [HttpGet("{tenantId:guid}/logo")]
    public async Task<IActionResult> Logo(Guid tenantId, CancellationToken ct)
    {
        if (!await IsAuthorizedAsync(tenantId, ct))
        {
            return NotFound();
        }

        var logo = _branding.GetLogo(tenantId);
        if (logo is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public, max-age=300";
        return PhysicalFile(logo.Value.Path, logo.Value.ContentType);
    }

    /// <summary>Only serve branding files for the currently-active tenant. Stops
    /// a curious admin probing for other tenants' assets on a shared install.</summary>
    private async Task<bool> IsAuthorizedAsync(Guid tenantId, CancellationToken ct)
    {
        var active = await _licenses.GetActiveAsync(ct);
        return active is not null && active.License.TenantId == tenantId;
    }
}
