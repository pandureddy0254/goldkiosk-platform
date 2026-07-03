namespace GoldKiosk.Kiosk.Api.Endpoints.Responses;

/// <summary>
/// State acknowledgement with the session's event sequence, returned by the offer
/// accept/decline commands (payload samples §5).
/// </summary>
/// <param name="State">The session state after the command.</param>
/// <param name="Sequence">The per-session monotonic event sequence.</param>
public sealed record StateSequenceResponse(string State, long Sequence);
