namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>Branding service.</summary>
public sealed class BrandingService : IBrandingService
{
    private static readonly (string FileName, string ContentType)[] _logoCandidates =
    {
        ("logo.svg",  "image/svg+xml"),
        ("logo.png",  "image/png"),
        ("logo.webp", "image/webp"),
        ("logo.jpg",  "image/jpeg"),
    };

    private readonly string _root;

    /// <summary>Initializes a new instance of the <see cref="BrandingService"/> class.</summary>
    public BrandingService(IHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "App_Data", "branding");
        Directory.CreateDirectory(_root);
    }

    /// <summary>Has branding for.</summary>
    public bool HasBrandingFor(License license)
    {
        if (!license.HasFeature("white_label"))
        {
            return false;
        }

        var css = GetBrandCssPath(license.TenantId);
        return css is not null || GetLogo(license.TenantId) is not null;
    }

    /// <summary>Get brand css path.</summary>
    public string? GetBrandCssPath(Guid tenantId)
    {
        var path = Path.Combine(_root, tenantId.ToString("D"), "brand.css");
        return File.Exists(path) ? path : null;
    }

    /// <summary>Get logo.</summary>
    public (string Path, string ContentType)? GetLogo(Guid tenantId)
    {
        var dir = Path.Combine(_root, tenantId.ToString("D"));
        foreach (var (name, type) in _logoCandidates)
        {
            var p = Path.Combine(dir, name);
            if (File.Exists(p))
            {
                return (p, type);
            }
        }
        return null;
    }
}
