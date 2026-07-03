namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>I branding service.</summary>
public interface IBrandingService
{
    /// <summary>True when white-label branding files exist for the tenant AND
    /// the tenant's license carries the white_label feature.</summary>
    bool HasBrandingFor(License license);

    /// <summary>Absolute path on disk to the tenant's brand.css, or null.</summary>
    string? GetBrandCssPath(Guid tenantId);

    /// <summary>Absolute path on disk to the tenant's logo image, or null. The
    /// returned tuple is (path, contentType). Looks for logo.svg, .png, .webp
    /// in that order.</summary>
    (string Path, string ContentType)? GetLogo(Guid tenantId);
}
