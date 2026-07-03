using System.Security.Claims;
using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Cloud.AdminPortal.Services.Common;

/// <summary>
/// HttpContext-backed <see cref="ICurrentUserService"/>.
/// Reads the user id, tenant id, and display name from the cookie principal.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    /// <summary>Tenant id claim type.</summary>
    public const string TenantIdClaimType = "tenant_id";

    private readonly IHttpContextAccessor _http;

    /// <summary>Initializes a new instance of the <see cref="CurrentUserService"/> class.</summary>
    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    /// <summary>Gets the user id.</summary>
    public Guid? UserId
    {
        get
        {
            var raw = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    /// <summary>Gets the tenant id.</summary>
    public Guid? TenantId
    {
        get
        {
            var raw = _http.HttpContext?.User.FindFirstValue(TenantIdClaimType);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    /// <summary>Find first value.</summary>
    public string? DisplayName =>
        _http.HttpContext?.User.FindFirstValue(ClaimTypes.GivenName) is string given &&
        _http.HttpContext?.User.FindFirstValue(ClaimTypes.Surname) is string family
            ? $"{given} {family}".Trim()
            : _http.HttpContext?.User.Identity?.Name;

    /// <summary>Gets a value indicating whether is authenticated.</summary>
    public bool IsAuthenticated =>
        _http.HttpContext?.User.Identity?.IsAuthenticated == true;
}
