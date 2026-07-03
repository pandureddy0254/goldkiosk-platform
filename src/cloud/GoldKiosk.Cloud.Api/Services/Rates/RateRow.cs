namespace GoldKiosk.Cloud.Api.Services.Rates;

/// <summary>
/// A read-model projection of one <c>pricing.metal_rates</c> row.
/// </summary>
/// <param name="Metal">The metal.</param>
/// <param name="PurityKarat">The purity in karat.</param>
/// <param name="PricePerGram">The price per gram in <paramref name="Currency"/>.</param>
/// <param name="Currency">The ISO 4217 currency.</param>
/// <param name="RetrievedAt">When the rate was sourced.</param>
public sealed record RateRow(
    string Metal,
    decimal PurityKarat,
    decimal PricePerGram,
    string Currency,
    DateTimeOffset RetrievedAt);
