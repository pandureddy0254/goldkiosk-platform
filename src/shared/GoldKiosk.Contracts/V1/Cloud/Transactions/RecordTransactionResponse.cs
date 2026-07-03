namespace GoldKiosk.Contracts.V1.Cloud.Transactions;

/// <summary>
/// Response body for <c>POST /api/v1/transactions</c>. Replaying the same
/// <paramref name="TransactionId"/> returns the original record unchanged.
/// </summary>
/// <param name="TransactionId">The recorded transaction id (the kiosk's id).</param>
/// <param name="CustomerId">The customer row created (placeholder until KYC enrichment).</param>
/// <param name="TransactionCode">The human-readable transaction code, e.g. <c>GK-20260703-1A2B3C4D</c>.</param>
public sealed record RecordTransactionResponse(
    Guid TransactionId,
    Guid CustomerId,
    string TransactionCode);
