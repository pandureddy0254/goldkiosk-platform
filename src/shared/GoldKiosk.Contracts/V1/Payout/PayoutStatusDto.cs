namespace GoldKiosk.Contracts.V1.Payout;

/// <summary>
/// The confirmed payout status.
/// </summary>
/// <param name="Method">The confirmed payout method.</param>
/// <param name="BillMixOk">Whether the cassettes can cover the amount; only present for cash payouts.</param>
public sealed record PayoutStatusDto(string Method, bool? BillMixOk);
