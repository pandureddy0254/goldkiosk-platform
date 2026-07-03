namespace GoldKiosk.Kiosk.Core.Cloud;

/// <summary>
/// The edge's single narrow port to Cloud.Api: pull provisioning before trading, fetch a
/// binding offer during a transaction, and forward completed transactions from the outbox.
/// Every method degrades gracefully — a <see langword="null"/> or <see langword="false"/>
/// result means "cloud unavailable" and the caller falls back to the local mock / retries
/// later. Implementations never throw across this port for an unreachable or erroring cloud;
/// only a caller-driven <see cref="CancellationToken"/> cancellation propagates.
/// </summary>
public interface ICloudGateway
{
    /// <summary>
    /// Pulls the kiosk's tenant/kiosk identity, live/trading status and trading window from
    /// the cloud (kiosk-login → config → status).
    /// </summary>
    /// <param name="cancellationToken">Cancels the pull.</param>
    /// <returns>The provisioning facts, or <see langword="null"/> when the cloud is unreachable.</returns>
    Task<KioskProvisioning?> GetProvisioningAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches a binding offer for an analysed item.
    /// </summary>
    /// <param name="input">The measured item facts.</param>
    /// <param name="cancellationToken">Cancels the fetch.</param>
    /// <returns>The cloud offer, or <see langword="null"/> when the cloud is unreachable or declines.</returns>
    Task<CloudOffer?> GetOfferAsync(CloudOfferInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads one completed transaction (record + any non-PII images) idempotently.
    /// </summary>
    /// <param name="upload">The completed transaction to forward.</param>
    /// <param name="cancellationToken">Cancels the upload.</param>
    /// <returns><see langword="true"/> when the cloud accepted the record; <see langword="false"/> to retry later.</returns>
    Task<bool> UploadTransactionAsync(TransactionUpload upload, CancellationToken cancellationToken = default);
}
