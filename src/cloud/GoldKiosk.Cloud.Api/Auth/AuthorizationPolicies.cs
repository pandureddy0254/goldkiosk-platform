namespace GoldKiosk.Cloud.Api.Auth;

/// <summary>
/// Authorization policy names. Every endpoint declares its policy explicitly
/// (security standard §Authentication &amp; authorization).
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Requires an authenticated kiosk-machine principal (<c>role = kiosk</c>).</summary>
    public const string Kiosk = "kiosk";
}
