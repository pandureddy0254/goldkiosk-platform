using GoldKiosk.Contracts.V1.Cloud.Transactions;

namespace GoldKiosk.Cloud.Api.Services.Transactions;

/// <summary>
/// Records completed kiosk transactions into the <c>tx</c> schema
/// (replaces legacy <c>MAKE-SELL</c>). Idempotent on the kiosk's transaction id.
/// </summary>
public interface ITransactionRecorder
{
    /// <summary>Records a completed kiosk transaction (or replays an existing record).</summary>
    /// <param name="request">The kiosk's completed transaction facts.</param>
    /// <param name="kioskId">The authenticated kiosk id.</param>
    /// <param name="tenantId">The kiosk's tenant id.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The recorded (or replayed) transaction identifiers.</returns>
    Task<RecordTransactionResponse> RecordAsync(
        RecordTransactionRequest request,
        Guid kioskId,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
