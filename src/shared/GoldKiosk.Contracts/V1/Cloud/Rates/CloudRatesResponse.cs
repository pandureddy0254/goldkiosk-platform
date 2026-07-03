namespace GoldKiosk.Contracts.V1.Cloud.Rates;

/// <summary>
/// Response body for <c>GET /api/v1/rates</c> on the Cloud API (replaces legacy
/// <c>GET_RATES</c>). Rates are the last-known good rows — the sync worker keeps them
/// fresh; a feed outage degrades to these values rather than fabricating prices.
/// </summary>
/// <param name="AsOf">The most recent <c>retrieved_at</c> across the returned rows.</param>
/// <param name="Currency">The ISO 4217 currency the rates are quoted in.</param>
/// <param name="Rates">The per-metal/purity rate rows.</param>
public sealed record CloudRatesResponse(
    DateTimeOffset AsOf,
    string Currency,
    IReadOnlyList<CloudMetalRateDto> Rates);
