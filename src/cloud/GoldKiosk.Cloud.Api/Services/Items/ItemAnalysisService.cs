using GoldKiosk.Cloud.Api.Logging;
using GoldKiosk.Cloud.Api.Options;
using GoldKiosk.Cloud.Api.Services.Ai;
using GoldKiosk.Contracts.V1.Cloud.Items;
using GoldKiosk.Contracts.V1.Common;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.Api.Services.Items;

/// <summary>
/// AI item verification decision mapping (design doc <c>ai-item-verification.md</c>,
/// binding rules):
/// <list type="number">
/// <item><description>Degrade-open ONLY on AI transport failure (accepting a real item is low-risk).</description></item>
/// <item><description><c>anormal</c> → rejected with a stable reason code (multiple items / wrong type / empty tray / unidentified).</description></item>
/// <item><description>Confidence below the threshold → <c>requires_live_agent</c>; a low-confidence
/// verdict is never silently approved (the legacy forced-approve pattern is banned).</description></item>
/// </list>
/// </summary>
/// <param name="classifier">The goldkiosk-ai client.</param>
/// <param name="aiOptions">The AI thresholds.</param>
/// <param name="logger">The host logger.</param>
public sealed class ItemAnalysisService(
    IItemClassifier classifier,
    IOptions<AiServiceOptions> aiOptions,
    ILogger<ItemAnalysisService> logger) : IItemAnalysisService
{
    /// <inheritdoc />
    public async Task<AnalyzeItemResponse> AnalyzeAsync(
        ItemImageUpload image,
        string category,
        string transactionType,
        string metalType,
        string? detailsByUserJson,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        AnomalyDetection? verdict = await classifier.DetectAnomalyAsync(
            image,
            transactionType,
            metalType,
            objectType: category.ToLowerInvariant(),
            detailsByUserJson,
            cancellationToken);

        if (verdict is null)
        {
            // Rule 1: transport failure only — accept and continue; the physical analysis
            // pipeline (weight, XRF) still gates the transaction downstream.
            logger.ItemAnalysisDegradedOpen();
            return new AnalyzeItemResponse(
                Accepted: true,
                DetectedCategory: null,
                RejectReason: null,
                RequiresLiveAgent: false,
                Confidence: 0.0);
        }

        double threshold = aiOptions.Value.MinConfidence;
        bool lowConfidence = verdict.Confidence < threshold;
        bool accepted = verdict.IsNormal && !lowConfidence;
        string? rejectReason = verdict.IsNormal ? null : MapRejectReason(verdict);

        var response = new AnalyzeItemResponse(
            Accepted: accepted,
            DetectedCategory: string.IsNullOrWhiteSpace(verdict.Label) ? null : verdict.Label,
            RejectReason: rejectReason,
            RequiresLiveAgent: lowConfidence,
            Confidence: verdict.Confidence);

        logger.ItemAnalyzed(verdict.Status, verdict.Label, verdict.Confidence, response.RequiresLiveAgent);
        return response;
    }

    private static string MapRejectReason(AnomalyDetection verdict)
    {
        string label = verdict.Label.ToUpperInvariant();
        string? found = verdict.FoundType?.ToUpperInvariant();

        if (label.Contains("MULTIPLE", StringComparison.Ordinal)
            || label.Contains("OVERLAP", StringComparison.Ordinal)
            || found?.Contains("MULTIPLE", StringComparison.Ordinal) == true)
        {
            return RejectionReasonCodes.ItemMultipleItems;
        }

        if (label.Contains("EMPTY", StringComparison.Ordinal)
            || label.Contains("NO_OBJECT", StringComparison.Ordinal))
        {
            return RejectionReasonCodes.ItemEmptyTray;
        }

        if (!string.IsNullOrWhiteSpace(verdict.ExpectedType)
            && !string.IsNullOrWhiteSpace(verdict.FoundType)
            && !string.Equals(verdict.ExpectedType, verdict.FoundType, StringComparison.OrdinalIgnoreCase))
        {
            return RejectionReasonCodes.ItemUnacceptedType;
        }

        return RejectionReasonCodes.ItemUnidentified;
    }
}
