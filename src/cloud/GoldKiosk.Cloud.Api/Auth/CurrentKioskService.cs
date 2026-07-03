using System.Security.Claims;
using GoldKiosk.Infrastructure.Common;

namespace GoldKiosk.Cloud.Api.Auth;

/// <summary>
/// Resolves the authenticated kiosk principal from the current HTTP request. Implements
/// <see cref="ICurrentUserService"/> so the Infrastructure
/// <c>TenantContextInterceptor</c> sets the RLS tenant context (<c>app.tenant_id</c>)
/// from the kiosk JWT's <c>tenant_id</c> claim on every connection. Outside a request
/// (background workers) all values are <see langword="null"/> and the interceptor skips.
/// </summary>
/// <param name="httpContextAccessor">Accessor for the ambient HTTP context.</param>
public sealed class CurrentKioskService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    /// <summary>Gets the authenticated kiosk id (the token's <c>sub</c> claim).</summary>
    public Guid? KioskId => ParseGuid(Principal?.FindFirst(KioskClaimTypes.Subject)?.Value);

    /// <summary>Gets the kiosk's fleet code from the token.</summary>
    public string? KioskCode => Principal?.FindFirst(KioskClaimTypes.KioskCode)?.Value;

    /// <inheritdoc />
    public Guid? UserId => KioskId;

    /// <inheritdoc />
    public Guid? TenantId => ParseGuid(Principal?.FindFirst(KioskClaimTypes.TenantId)?.Value);

    /// <inheritdoc />
    public string? DisplayName => KioskCode;

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    private static Guid? ParseGuid(string? value) =>
        Guid.TryParse(value, out Guid parsed) ? parsed : null;
}
