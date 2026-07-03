namespace GoldKiosk.Contracts.V1.Common;

/// <summary>
/// The money envelope on the wire: minor units, ISO currency, and a preformatted display string.
/// </summary>
/// <param name="AmountMinor">The amount in the currency's minor units (e.g. cents for USD).</param>
/// <param name="Currency">The ISO 4217 currency code, e.g. <c>USD</c>.</param>
/// <param name="Display">The preformatted display string, e.g. <c>$8,460.00</c>.</param>
public sealed record MoneyDto(long AmountMinor, string Currency, string Display);
