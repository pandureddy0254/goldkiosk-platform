using GoldKiosk.Contracts.V1.Common;

namespace GoldKiosk.Contracts.V1.Cloud.Rates;

/// <summary>
/// One metal/purity rate row served to kiosks (sourced from <c>pricing.metal_rates</c>).
/// </summary>
/// <param name="Metal">The metal, e.g. <c>gold</c>, <c>silver</c>.</param>
/// <param name="PurityKarat">The purity the rate applies to, in karat (24 = fine).</param>
/// <param name="PerGram">The price per gram.</param>
/// <param name="RetrievedAt">When this rate was sourced from the market feed.</param>
public sealed record CloudMetalRateDto(
    string Metal,
    decimal PurityKarat,
    MoneyDto PerGram,
    DateTimeOffset RetrievedAt);
