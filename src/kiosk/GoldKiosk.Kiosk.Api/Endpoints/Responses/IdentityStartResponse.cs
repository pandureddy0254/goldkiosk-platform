namespace GoldKiosk.Kiosk.Api.Endpoints.Responses;

/// <summary>
/// Acknowledgement for <c>POST /sessions/{id}/identity/start</c> (payload samples §6);
/// step progress then flows over SignalR.
/// </summary>
/// <param name="State">The session state (<c>identity</c>).</param>
/// <param name="Identity">The current identity step.</param>
public sealed record IdentityStartResponse(string State, IdentityCurrentStepDto Identity);
