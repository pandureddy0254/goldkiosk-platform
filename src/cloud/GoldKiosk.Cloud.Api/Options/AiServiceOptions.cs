using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.Api.Options;

/// <summary>
/// In-house goldkiosk-ai HTTP service settings (owner decision: item classification uses
/// this service — no LLM SDKs). Wire contract mirrors the legacy
/// <c>GoldKioskAiApiProvider</c>: anomaly detection, gold detection, health check.
/// </summary>
public sealed class AiServiceOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "AiService";

    /// <summary>Gets the service base URL (versioned root).</summary>
    [Required(AllowEmptyStrings = false)]
    [Url]
    public string BaseUrl { get; init; } = "https://goldkiosk-ai.smartlawyer.ai/api/v1";

    /// <summary>Gets the per-request timeout in seconds (image analysis can be slow).</summary>
    [Range(10, 600)]
    public int TimeoutSeconds { get; init; } = 120;

    /// <summary>
    /// Gets the confidence threshold below which a verdict escalates to a live agent
    /// instead of standing on its own (never silently approved).
    /// </summary>
    [Range(0.0, 1.0)]
    public double MinConfidence { get; init; } = 0.75;

    /// <summary>Gets the vision model selector for gold detection (<c>openai</c> | <c>mistral</c> | <c>qwen</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    public string GoldDetectionModel { get; init; } = "openai";

    /// <summary>Gets the maximum accepted upload size per image in bytes.</summary>
    [Range(1024, 50 * 1024 * 1024)]
    public long MaxImageBytes { get; init; } = 10 * 1024 * 1024;
}
