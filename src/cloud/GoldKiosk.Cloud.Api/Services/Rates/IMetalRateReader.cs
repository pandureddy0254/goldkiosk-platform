namespace GoldKiosk.Cloud.Api.Services.Rates;

/// <summary>
/// Reads last-known metal rates from <c>pricing.metal_rates</c> (the degrade target when
/// the live feed is down — stale-but-honest, never fabricated).
/// </summary>
public interface IMetalRateReader
{
    /// <summary>Gets the latest rate row for a metal/karat in a currency.</summary>
    /// <param name="metal">The metal, e.g. <c>gold</c>.</param>
    /// <param name="purityKarat">The purity in karat.</param>
    /// <param name="currency">The ISO 4217 currency.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The latest rate, or <see langword="null"/> when none exists.</returns>
    Task<RateRow?> GetLatestAsync(
        string metal, decimal purityKarat, string currency, CancellationToken cancellationToken = default);

    /// <summary>Gets the latest rate row per metal/karat combination in a currency.</summary>
    /// <param name="currency">The ISO 4217 currency.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>The latest rows, empty when no rates exist yet.</returns>
    Task<IReadOnlyList<RateRow>> GetLatestPerMetalAsync(
        string currency, CancellationToken cancellationToken = default);
}
