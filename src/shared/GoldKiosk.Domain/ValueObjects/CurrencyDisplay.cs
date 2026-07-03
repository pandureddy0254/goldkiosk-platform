using System.Globalization;

namespace GoldKiosk.Domain.ValueObjects;

/// <summary>
/// Invariant display formatting and minor-unit metadata for ISO 4217 currencies.
/// </summary>
public static class CurrencyDisplay
{
    /// <summary>
    /// Gets the number of minor-unit digits for a currency (ISO 4217 exponent).
    /// Unknown currencies default to 2.
    /// </summary>
    /// <param name="currencyCode">The uppercase ISO 4217 currency code.</param>
    /// <returns>The minor-unit digit count (0, 2 or 3).</returns>
    public static int GetMinorUnitDigits(string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        return currencyCode switch
        {
            "BHD" or "IQD" or "JOD" or "KWD" or "LYD" or "OMR" or "TND" => 3,
            "JPY" or "KRW" or "VND" => 0,
            _ => 2,
        };
    }

    /// <summary>
    /// Formats a monetary value invariantly with symbol and group separators,
    /// e.g. <c>$8,460.00</c> for USD. Currencies without a known symbol render as
    /// <c>AED 12.34</c>.
    /// </summary>
    /// <param name="money">The monetary value to format.</param>
    /// <returns>The display string.</returns>
    public static string Format(Money money)
    {
        ArgumentNullException.ThrowIfNull(money);
        var digits = GetMinorUnitDigits(money.CurrencyCode);
        var format = "N" + digits.ToString(CultureInfo.InvariantCulture);
        var magnitude = Math.Abs(money.Amount).ToString(format, CultureInfo.InvariantCulture);
        var sign = money.Amount < 0 ? "-" : string.Empty;
        var symbol = GetSymbol(money.CurrencyCode);
        return symbol is null
            ? $"{sign}{money.CurrencyCode} {magnitude}"
            : $"{sign}{symbol}{magnitude}";
    }

    private static string? GetSymbol(string currencyCode) => currencyCode switch
    {
        "USD" => "$",
        "EUR" => "€",
        "GBP" => "£",
        "INR" => "₹",
        "JPY" => "¥",
        _ => null,
    };
}
