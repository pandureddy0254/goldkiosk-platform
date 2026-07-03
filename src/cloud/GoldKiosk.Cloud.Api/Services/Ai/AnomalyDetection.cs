namespace GoldKiosk.Cloud.Api.Services.Ai;

/// <summary>
/// The goldkiosk-ai anomaly-detection verdict (wire: <c>POST /detections/anomaly</c>).
/// Mirrors the legacy <c>AnomalyDetectionResult</c> contract.
/// </summary>
/// <param name="Label">The detected object label, e.g. <c>ring</c>, <c>multiple_items</c>.</param>
/// <param name="Confidence">The verdict confidence (0.0–1.0).</param>
/// <param name="Status">The verdict status: <c>normal</c> (one matching item) or <c>anormal</c>.</param>
/// <param name="ExpectedType">The expected object type, when the service reports a mismatch.</param>
/// <param name="FoundType">The object type actually found, when reported.</param>
/// <param name="Notes">Free-form service notes, when reported.</param>
public sealed record AnomalyDetection(
    string Label,
    double Confidence,
    string Status,
    string? ExpectedType,
    string? FoundType,
    string? Notes)
{
    /// <summary>Gets a value indicating whether the verdict is normal (one matching item detected).</summary>
    public bool IsNormal => string.Equals(Status, "normal", StringComparison.OrdinalIgnoreCase);
}
