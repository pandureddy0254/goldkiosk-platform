using System.Security.Claims;
using GoldKiosk.Cloud.CRMPortal.Models.Domain;

namespace GoldKiosk.Cloud.CRMPortal.Services;

/// <summary>
/// HttpContext-backed <see cref="ICurrentUserService"/>. Reads identity from the
/// cookie principal's claims (set by <see cref="CrmAuthService"/> at sign-in).
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    /// <summary>Initializes the service with the ambient HTTP context accessor.</summary>
    /// <param name="http">Accessor for the current request's context.</param>
    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    /// <inheritdoc/>
    public Guid? UserId =>
        Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    /// <inheritdoc/>
    public string? Role => User?.FindFirstValue(ClaimTypes.Role);

    /// <inheritdoc/>
    public string? FullName => User?.FindFirstValue(ClaimTypes.Name);

    /// <inheritdoc/>
    public string? Email => User?.FindFirstValue(ClaimTypes.Email);

    /// <inheritdoc/>
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    /// <inheritdoc/>
    public bool IsManager => Role is CrmRole.SalesManager or CrmRole.Superadmin;

    /// <inheritdoc/>
    public bool IsSuperadmin => Role == CrmRole.Superadmin;
}
