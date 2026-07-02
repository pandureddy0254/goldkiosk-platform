namespace GoldKiosk.Contracts.V1.Payout;

/// <summary>
/// Request body for <c>POST /api/v1/sessions/{id}/payout</c> — method and bank details
/// clubbed into one call.
/// </summary>
/// <param name="Method">The payout method: <c>cash</c>, <c>bank_transfer</c> or <c>debit_card</c>.</param>
/// <param name="Bank">The bank details; required when <paramref name="Method"/> is <c>bank_transfer</c>.</param>
public sealed record PayoutRequest(string Method, BankDetailsDto? Bank);
