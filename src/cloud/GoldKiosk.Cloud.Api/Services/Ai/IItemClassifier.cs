namespace GoldKiosk.Cloud.Api.Services.Ai;

/// <summary>
/// Port to the in-house goldkiosk-ai HTTP service (owner decision — item classification
/// uses this service, not an LLM SDK). All methods return <see langword="null"/> on
/// transport failure so callers can apply the degrade-open policy explicitly.
/// </summary>
public interface IItemClassifier
{
    /// <summary>Runs anomaly detection on a tray image (one item, right type, no overlap).</summary>
    /// <param name="image">The tray image.</param>
    /// <param name="transactionType">The transaction type, e.g. <c>sell</c> or <c>pawn</c>.</param>
    /// <param name="metalType">The expected metal, e.g. <c>gold</c>.</param>
    /// <param name="objectType">The expected object type, e.g. <c>ring</c>; <c>other</c> when unknown.</param>
    /// <param name="detailsByUserJson">Optional user-declared details as a JSON string.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The verdict, or <see langword="null"/> on transport/service failure.</returns>
    Task<AnomalyDetection?> DetectAnomalyAsync(
        ItemImageUpload image,
        string transactionType,
        string metalType,
        string objectType,
        string? detailsByUserJson,
        CancellationToken cancellationToken = default);

    /// <summary>Estimates gold probability and karat from a tray image.</summary>
    /// <param name="image">The tray image.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The estimate, or <see langword="null"/> on transport/service failure.</returns>
    Task<GoldDetection?> AnalyzeGoldAsync(
        ItemImageUpload image, CancellationToken cancellationToken = default);

    /// <summary>Checks whether the AI service reports itself live.</summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns><see langword="true"/> when the service health endpoint reports live.</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
