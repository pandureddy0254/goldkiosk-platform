using GoldKiosk.Contracts.V1.Payout;

namespace GoldKiosk.Kiosk.Core.Sessions;

/// <summary>
/// The customer's confirmed payout selection held by the session. Bank details are
/// restricted data: kept in memory for the payout attempt only and never written to the
/// journal or logs (ADR 0002 PII hardening).
/// </summary>
/// <param name="Method">The payout method: <c>cash</c>, <c>bank_transfer</c> or <c>debit_card</c>.</param>
/// <param name="Bank">The bank details for a transfer; never persisted or logged.</param>
/// <param name="PlannedBills">The planned bill mix (denomination → count) for a cash payout.</param>
/// <param name="BillMixOk">Whether the cassettes can cover the amount; only meaningful for cash.</param>
public sealed record PayoutSelection(
    string Method,
    BankDetailsDto? Bank,
    IReadOnlyDictionary<int, int>? PlannedBills,
    bool? BillMixOk);
