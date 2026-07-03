using GoldKiosk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoldKiosk.Cloud.Api.Services.Rates;

/// <summary>
/// EF-backed <see cref="IMetalRateReader"/> over <c>pricing.metal_rates</c>
/// (read-only, no tracking, newest row per metal/karat).
/// </summary>
/// <param name="db">The platform database.</param>
public sealed class MetalRateReader(AppDbContext db) : IMetalRateReader
{
    /// <inheritdoc />
    public async Task<RateRow?> GetLatestAsync(
        string metal, decimal purityKarat, string currency, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(metal);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        return await db.MetalRates
            .AsNoTracking()
            .Where(r => r.Metal == metal && r.PurityKarat == purityKarat && r.CurrencyCode == currency)
            .OrderByDescending(r => r.RetrievedAt)
            .Select(r => new RateRow(r.Metal, r.PurityKarat, r.PricePerGram, r.CurrencyCode, r.RetrievedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RateRow>> GetLatestPerMetalAsync(
        string currency, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        // The combination set is tiny (gold 24/22/18 + silver 24 today), so a distinct
        // pass followed by one newest-row query per combination stays simple and cheap.
        var combos = await db.MetalRates
            .AsNoTracking()
            .Where(r => r.CurrencyCode == currency)
            .Select(r => new { r.Metal, r.PurityKarat })
            .Distinct()
            .ToListAsync(cancellationToken);

        List<RateRow> rows = new(combos.Count);
        foreach (var combo in combos
            .OrderBy(c => c.Metal, StringComparer.Ordinal)
            .ThenByDescending(c => c.PurityKarat))
        {
            RateRow? row = await GetLatestAsync(combo.Metal, combo.PurityKarat, currency, cancellationToken);
            if (row is not null)
            {
                rows.Add(row);
            }
        }

        return rows;
    }
}
