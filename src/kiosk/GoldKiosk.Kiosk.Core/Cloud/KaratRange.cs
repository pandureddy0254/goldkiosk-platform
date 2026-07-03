namespace GoldKiosk.Kiosk.Core.Cloud;

/// <summary>
/// The karat acceptance window a kiosk trades within, sourced from the cloud config pull
/// (replaces legacy <c>KARAT-RANGE-PERCENTAGE</c>). Framework-free carrier for the port.
/// </summary>
/// <param name="MinKarat">The minimum accepted karat.</param>
/// <param name="MaxKarat">The maximum accepted karat.</param>
public sealed record KaratRange(decimal MinKarat, decimal MaxKarat);
