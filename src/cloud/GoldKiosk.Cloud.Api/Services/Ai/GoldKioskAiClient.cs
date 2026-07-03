using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using GoldKiosk.Cloud.Api.Logging;
using GoldKiosk.Cloud.Api.Options;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.Api.Services.Ai;

/// <summary>
/// Typed HTTP client for the in-house goldkiosk-ai service. Wire contract adapted from
/// the legacy <c>GoldKioskAiApiProvider</c> (GoldCube.Store): multipart uploads to
/// <c>/detections/anomaly</c> and <c>/gold-detection/analyze</c>, JSON verdicts, and
/// <c>/health/check</c>. Transport failures return <see langword="null"/> — the caller
/// owns the degrade policy.
/// </summary>
/// <param name="httpClient">The configured HTTP client (base address + 120 s timeout).</param>
/// <param name="aiOptions">The AI service settings.</param>
/// <param name="logger">The host logger.</param>
public sealed class GoldKioskAiClient(
    HttpClient httpClient,
    IOptions<AiServiceOptions> aiOptions,
    ILogger<GoldKioskAiClient> logger) : IItemClassifier
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<AnomalyDetection?> DetectAnomalyAsync(
        ItemImageUpload image,
        string transactionType,
        string metalType,
        string objectType,
        string? detailsByUserJson,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(metalType);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectType);

        using var form = new MultipartFormDataContent();
        AddImage(form, image);
        form.Add(new StringContent(transactionType), "transaction_type");
        form.Add(new StringContent(metalType), "metal_type");
        form.Add(new StringContent(objectType), "object_type");
        if (!string.IsNullOrWhiteSpace(detailsByUserJson))
        {
            form.Add(new StringContent(detailsByUserJson), "details_by_user");
        }

        AnomalyWireResult? wire = await PostAsync<AnomalyWireResult>(
            "detections/anomaly", form, cancellationToken);
        if (wire is null)
        {
            return null;
        }

        return new AnomalyDetection(
            Label: wire.Label ?? string.Empty,
            Confidence: wire.Confidence,
            Status: wire.Status ?? string.Empty,
            ExpectedType: DetailString(wire.Details, "expected_type"),
            FoundType: DetailString(wire.Details, "found_type"),
            Notes: DetailString(wire.Details, "notes"));
    }

    /// <inheritdoc />
    public async Task<GoldDetection?> AnalyzeGoldAsync(
        ItemImageUpload image, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);

        using var form = new MultipartFormDataContent();
        AddImage(form, image);

        string model = Uri.EscapeDataString(aiOptions.Value.GoldDetectionModel);
        GoldWireResult? wire = await PostAsync<GoldWireResult>(
            $"gold-detection/analyze?model={model}", form, cancellationToken);
        if (wire is null)
        {
            return null;
        }

        return new GoldDetection(
            wire.GoldProbability, wire.Confidence, wire.EstimatedKarat, wire.KaratConfidence);
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response =
                await httpClient.GetAsync(new Uri("health/check", UriKind.Relative), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.AiRequestFailed("health/check", (int)response.StatusCode);
                return false;
            }

            HealthWireResult? health = await response.Content
                .ReadFromJsonAsync<HealthWireResult>(JsonOptions, cancellationToken);
            return string.Equals(health?.Status, "live", StringComparison.OrdinalIgnoreCase);
        }
        catch (HttpRequestException ex)
        {
            logger.AiTransportFailure(ex, "health/check");
            return false;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.AiTransportFailure(ex, "health/check");
            return false;
        }
    }

    private async Task<T?> PostAsync<T>(
        string relativeUrl, MultipartFormDataContent form, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using HttpResponseMessage response = await httpClient.PostAsync(
                new Uri(relativeUrl, UriKind.Relative), form, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.AiRequestFailed(relativeUrl, (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.AiTransportFailure(ex, relativeUrl);
            return null;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Client timeout, not a host shutdown: transport failure by policy.
            logger.AiTransportFailure(ex, relativeUrl);
            return null;
        }
        catch (JsonException ex)
        {
            logger.AiTransportFailure(ex, relativeUrl);
            return null;
        }
    }

    private static void AddImage(MultipartFormDataContent form, ItemImageUpload image)
    {
        var content = new ByteArrayContent(image.Content.ToArray());
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(image.ContentType);
        form.Add(content, "file", image.FileName);
    }

    private static string? DetailString(Dictionary<string, JsonElement>? details, string key)
    {
        if (details is null || !details.TryGetValue(key, out JsonElement element))
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.GetRawText(),
        };
    }

    private sealed record AnomalyWireResult
    {
        [JsonPropertyName("label")]
        public string? Label { get; init; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("details")]
        public Dictionary<string, JsonElement>? Details { get; init; }
    }

    private sealed record GoldWireResult
    {
        [JsonPropertyName("gold_probability")]
        public double GoldProbability { get; init; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; init; }

        [JsonPropertyName("estimated_karat")]
        public int? EstimatedKarat { get; init; }

        [JsonPropertyName("karat_confidence")]
        public double KaratConfidence { get; init; }
    }

    private sealed record HealthWireResult
    {
        [JsonPropertyName("status")]
        public string? Status { get; init; }
    }
}
