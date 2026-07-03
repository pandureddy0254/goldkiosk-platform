using GoldKiosk.Contracts.V1.Payout;

namespace GoldKiosk.Kiosk.Api.Endpoints.Responses;

/// <summary>
/// Acknowledgement for <c>POST /sessions/{id}/payout</c> (payload samples §8).
/// </summary>
/// <param name="State">The session state after confirmation (<c>payout_confirmed</c>).</param>
/// <param name="Payout">The confirmed payout status.</param>
public sealed record PayoutConfirmedResponse(string State, PayoutStatusDto Payout);
