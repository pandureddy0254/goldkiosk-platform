namespace GoldKiosk.Cloud.Api.Services.Rates;

/// <summary>
/// Fetches current spot prices from the external market feed (GoldAPI.io).
/// </summary>
public interface IMetalPriceProvider
{
    /// <summary>Fetches the current gold + silver per-gram prices.</summary>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The fetched rates, or <see langword="null"/> when the feed is unavailable
    /// or unconfigured (callers degrade to last-known DB rates).</returns>
    Task<SpotRates?> FetchAsync(CancellationToken cancellationToken = default);
}
