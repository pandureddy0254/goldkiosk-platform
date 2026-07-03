namespace GoldKiosk.Kiosk.Api.Endpoints.Responses;

/// <summary>
/// The current identity step block of the identity-start acknowledgement
/// (payload samples §6).
/// </summary>
/// <param name="CurrentStep">The identity step now in progress, e.g. <c>id_scan</c>.</param>
public sealed record IdentityCurrentStepDto(string CurrentStep);
