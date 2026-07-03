using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>
/// First-line license check. Runs after authentication so the auth claims are
/// available, but before MVC routing. If the install has no bound license, or
/// the bound license has expired/been revoked, the user is sent to /activate.
///
/// Bypassed for: /License/*, /activate, /Account/* (login/logout), /branding/*,
/// static files, and the error endpoint — anything that needs to render while
/// unlicensed.
/// </summary>
public sealed class LicenseGateMiddleware
{
    private static readonly string[] _bypassPrefixes =
    {
        "/License",
        "/activate",
        "/Account",
        "/branding",
        "/css",
        "/js",
        "/img",
        "/lib",
        "/favicon",
        "/Home/Error",
        "/_framework",
        "/_vs",
        "/health",
    };

    private readonly RequestDelegate _next;
    private readonly IOptions<LicensingOptions> _options;

    /// <summary>Initializes a new instance of the <see cref="LicenseGateMiddleware"/> class.</summary>
    public LicenseGateMiddleware(RequestDelegate next, IOptions<LicensingOptions> options)
    {
        _next = next;
        _options = options;
    }

    /// <summary>Invoke.</summary>
    public async Task InvokeAsync(HttpContext ctx, ILicenseService licenses)
    {
        if (!_options.Value.EnforceLicense)
        {
            await _next(ctx);
            return;
        }

        var path = ctx.Request.Path.Value ?? "";
        foreach (var prefix in _bypassPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                await _next(ctx);
                return;
            }
        }

        if (await licenses.IsActiveValidAsync(ctx.RequestAborted))
        {
            await _next(ctx);
            return;
        }

        ctx.Response.Redirect("/License/Activate");
    }
}
