using GoldKiosk.Contracts.V1.Common;

namespace GoldKiosk.Contracts.V1.Rates;

/// <summary>
/// A single metal's display rate on the attract ticker.
/// </summary>
/// <param name="Metal">The metal, e.g. <c>gold</c>, <c>silver</c>.</param>
/// <param name="PerGram">The display price per gram.</param>
/// <param name="ChangePercent">The percentage change since the previous close, e.g. <c>0.42</c>.</param>
public sealed record MetalRateDto(string Metal, MoneyDto PerGram, decimal ChangePercent);
