using GoldKiosk.Contracts.V1.Common;

namespace GoldKiosk.Contracts.V1.Cloud.Transactions;

/// <summary>
/// Request body for <c>POST /api/v1/transactions</c> (replaces legacy <c>MAKE-SELL</c>) —
/// the completed kiosk transaction record. Idempotent on <paramref name="TransactionId"/>:
/// the kiosk generates the id once and replays safely from its outbox.
/// </summary>
/// <param name="TransactionId">The kiosk-generated transaction id (idempotency anchor).</param>
/// <param name="Kind">The transaction kind: <c>sale</c> or <c>pawn</c>.</param>
/// <param name="Metal">The assayed metal, e.g. <c>gold</c>, <c>silver</c>.</param>
/// <param name="Category">The item category key, e.g. <c>ring</c>.</param>
/// <param name="Karat">The assayed karat.</param>
/// <param name="WeightGrams">The measured weight in grams.</param>
/// <param name="PurityPercent">The assayed purity in percent (0–100).</param>
/// <param name="Amount">The settled payout amount.</param>
/// <param name="OfferId">The accepted offer's quote id, when available.</param>
public sealed record RecordTransactionRequest(
    Guid TransactionId,
    string Kind,
    string Metal,
    string Category,
    decimal Karat,
    decimal WeightGrams,
    decimal PurityPercent,
    MoneyDto Amount,
    Guid? OfferId);
