namespace GoldKiosk.Contracts.V1.Cloud.Kiosk;

/// <summary>
/// The karat acceptance window for a kiosk (replaces legacy <c>KARAT-RANGE-PERCENTAGE</c>).
/// </summary>
/// <param name="MinKarat">The minimum accepted karat.</param>
/// <param name="MaxKarat">The maximum accepted karat.</param>
public sealed record KaratRangeDto(decimal MinKarat, decimal MaxKarat);
