namespace GoldKiosk.Cloud.Api.Services.Ai;

/// <summary>
/// The goldkiosk-ai gold-detection result (wire: <c>POST /gold-detection/analyze</c>).
/// Mirrors the legacy <c>GoldDetectionResponse</c> contract.
/// </summary>
/// <param name="GoldProbability">The probability the item is gold (0.0–1.0).</param>
/// <param name="Confidence">The confidence in the visual assessment (0.0–1.0).</param>
/// <param name="EstimatedKarat">The estimated karat (10/14/18/22/24) when likely gold.</param>
/// <param name="KaratConfidence">The confidence in the karat estimate (0.0–1.0).</param>
public sealed record GoldDetection(
    double GoldProbability,
    double Confidence,
    int? EstimatedKarat,
    double KaratConfidence);
