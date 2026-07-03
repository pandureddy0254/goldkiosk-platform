namespace GoldKiosk.Cloud.Api.Auth;

/// <summary>
/// JWT claim names used on kiosk tokens. Inbound claim mapping is disabled
/// (<c>MapInboundClaims = false</c>) so these raw names flow end to end.
/// </summary>
public static class KioskClaimTypes
{
    /// <summary>The subject claim — carries the kiosk id.</summary>
    public const string Subject = "sub";

    /// <summary>The kiosk fleet code claim.</summary>
    public const string KioskCode = "kiosk_code";

    /// <summary>The tenant id claim — feeds the RLS tenant context.</summary>
    public const string TenantId = "tenant_id";

    /// <summary>The role claim name (set as <c>RoleClaimType</c> on token validation).</summary>
    public const string Role = "role";

    /// <summary>The role value identifying a kiosk machine principal.</summary>
    public const string KioskRoleValue = "kiosk";
}
