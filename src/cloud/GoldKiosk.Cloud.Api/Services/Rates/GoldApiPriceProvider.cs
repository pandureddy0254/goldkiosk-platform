using System.Text.Json.Serialization;
using GoldKiosk.Cloud.Api.Logging;
using GoldKiosk.Cloud.Api.Options;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Cloud.Api.Services.Rates;

/// <summary>
/// GoldAPI.io adapter (adapted from platform2's <c>GoldApiPriceProvider</c> — same feed,
/// same response shape). Fetches XAU + XAG per-gram prices in the deployment currency.
/// Failures return <see langword="null"/>: the platform degrades to last-known DB rates,
/// never fabricated ones.
/// </summary>
/// <param name="httpClientFactory">The HTTP client factory (named client <see cref="HttpClientName"/>).</param>
/// <param name="goldApiOptions">The feed settings.</param>
/// <param name="regionOptions">The deployment region (currency).</param>
/// <param name="logger">The host logger.</param>
public sealed class GoldApiPriceProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<GoldApiOptions> goldApiOptions,
    IOptions<RegionOptions> regionOptions,
    ILogger<GoldApiPriceProvider> logger) : IMetalPriceProvider
{
    /// <summary>The named <see cref="HttpClient"/> this provider uses.</summary>
    public const string HttpClientName = "goldapi";

    /// <inheritdoc />
    public async Task<SpotRates?> FetchAsync(CancellationToken cancellationToken = default)
    {
        GoldApiOptions options = goldApiOptions.Value;
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            logger.RateFetchSkippedNoApiKey();
            return null;
        }

        string currency = regionOptions.Value.DefaultCurrency;
        string baseUrl = options.BaseUrl.TrimEnd('/');

        try
        {
            HttpClient client = httpClientFactory.CreateClient(HttpClientName);
            client.DefaultRequestHeaders.Add("x-access-token", options.ApiKey);

            GoldApiResponse? gold = await client.GetFromJsonAsync<GoldApiResponse>(
                new Uri($"{baseUrl}/XAU/{currency}"), cancellationToken);
            GoldApiResponse? silver = await client.GetFromJsonAsync<GoldApiResponse>(
                new Uri($"{baseUrl}/XAG/{currency}"), cancellationToken);

            if (gold is null || silver is null)
            {
                logger.RateFetchReturnedNull();
                return null;
            }

            logger.RatesFetched(
                currency, gold.PriceGram24k, gold.PriceGram22k, gold.PriceGram18k, silver.PriceGram24k);

            return new SpotRates(
                Gold24kPerGram: gold.PriceGram24k,
                Gold22kPerGram: gold.PriceGram22k,
                Gold18kPerGram: gold.PriceGram18k,
                SilverPerGram: silver.PriceGram24k);
        }
        catch (HttpRequestException ex)
        {
            logger.RateFetchFailed(ex);
            return null;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient timeout (not a host shutdown) — degrade, don't crash the worker.
            logger.RateFetchFailed(ex);
            return null;
        }
        catch (System.Text.Json.JsonException ex)
        {
            logger.RateFetchFailed(ex);
            return null;
        }
    }

    private sealed record GoldApiResponse
    {
        [JsonPropertyName("price_gram_24k")]
        public decimal PriceGram24k { get; init; }

        [JsonPropertyName("price_gram_22k")]
        public decimal PriceGram22k { get; init; }

        [JsonPropertyName("price_gram_18k")]
        public decimal PriceGram18k { get; init; }
    }
}
