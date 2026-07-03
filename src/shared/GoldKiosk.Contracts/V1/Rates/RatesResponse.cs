namespace GoldKiosk.Contracts.V1.Rates;

/// <summary>
/// Response body for <c>GET /api/v1/rates</c> — the display-safe market ticker for the
/// attract loop.
/// </summary>
/// <param name="AsOf">When the rates were sourced.</param>
/// <param name="Rates">The per-metal rates.</param>
public sealed record RatesResponse(DateTimeOffset AsOf, IReadOnlyList<MetalRateDto> Rates);
