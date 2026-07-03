namespace GoldKiosk.Kiosk.Api.Endpoints.Responses;

/// <summary>
/// Minimal state acknowledgement returned by session commands whose wire contract carries
/// only the resulting state (signature, contact, settle, abort — payload samples §6–§10).
/// </summary>
/// <param name="State">The session state after the command.</param>
public sealed record StateResponse(string State);
