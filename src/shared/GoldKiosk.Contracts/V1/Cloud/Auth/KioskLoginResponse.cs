namespace GoldKiosk.Contracts.V1.Cloud.Auth;

/// <summary>
/// Response body for <c>POST /api/v1/auth/kiosk-login</c>.
/// </summary>
/// <param name="AccessToken">The signed JWT the kiosk presents as a bearer token.</param>
/// <param name="ExpiresAt">When the token expires; the kiosk re-authenticates before this.</param>
/// <param name="KioskId">The kiosk's fleet identifier.</param>
/// <param name="FriendlyName">The kiosk's operator-facing display name.</param>
public sealed record KioskLoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    Guid KioskId,
    string FriendlyName);
