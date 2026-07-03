namespace GoldKiosk.Domain.ValueObjects;

/// <summary>
/// A monetary amount in a single ISO 4217 currency.
/// </summary>
/// <remarks>
/// Rounding policy (defined once, here): amounts are normalized to the currency's
/// minor-unit scale (see <see cref="CurrencyDisplay.GetMinorUnitDigits"/>) using banker's
/// rounding (<see cref="MidpointRounding.ToEven"/>) when materialized via <see cref="From"/>
/// or <see cref="ToMinorUnits"/>. Offer rounding uses the house-favorable floor policy
/// exposed by <see cref="FloorToNearest"/>.
/// </remarks>
public sealed record Money
{
    private Money(decimal amount, string currencyCode)
    {
        Amount = amount;
        CurrencyCode = currencyCode;
    }

    /// <summary>Gets the amount in major units (e.g. dollars for USD).</summary>
    public decimal Amount { get; }

    /// <summary>Gets the ISO 4217 currency code, normalized to uppercase (e.g. <c>USD</c>).</summary>
    public string CurrencyCode { get; }

    /// <summary>
    /// Creates a <see cref="Money"/> from a major-unit amount, rounding to the currency's
    /// minor-unit scale with banker's rounding.
    /// </summary>
    /// <param name="amount">The amount in major units.</param>
    /// <param name="currencyCode">The ISO 4217 currency code (case-insensitive).</param>
    /// <returns>The normalized monetary value.</returns>
    public static Money From(decimal amount, string currencyCode)
    {
        var code = NormalizeCurrencyCode(currencyCode);
        var digits = CurrencyDisplay.GetMinorUnitDigits(code);
        return new Money(decimal.Round(amount, digits, MidpointRounding.ToEven), code);
    }

    /// <summary>
    /// Creates a <see cref="Money"/> from an amount expressed in the currency's minor units
    /// (e.g. cents for USD, fils for BHD, whole yen for JPY).
    /// </summary>
    /// <param name="amountMinor">The amount in minor units.</param>
    /// <param name="currencyCode">The ISO 4217 currency code (case-insensitive).</param>
    /// <returns>The monetary value.</returns>
    public static Money FromMinorUnits(long amountMinor, string currencyCode)
    {
        var code = NormalizeCurrencyCode(currencyCode);
        var digits = CurrencyDisplay.GetMinorUnitDigits(code);
        return new Money(amountMinor / Pow10(digits), code);
    }

    /// <summary>
    /// Converts the amount to the currency's minor units, applying banker's rounding when
    /// the amount carries more precision than the minor-unit scale.
    /// </summary>
    /// <returns>The amount in minor units.</returns>
    /// <exception cref="OverflowException">The scaled amount does not fit in an <see cref="long"/>.</exception>
    public long ToMinorUnits()
    {
        var digits = CurrencyDisplay.GetMinorUnitDigits(CurrencyCode);
        var scaled = decimal.Round(Amount * Pow10(digits), 0, MidpointRounding.ToEven);
        return decimal.ToInt64(scaled);
    }

    /// <summary>Adds another amount of the same currency.</summary>
    /// <param name="other">The amount to add.</param>
    /// <returns>The sum.</returns>
    /// <exception cref="InvalidOperationException">The currencies differ.</exception>
    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, CurrencyCode);
    }

    /// <summary>Subtracts another amount of the same currency.</summary>
    /// <param name="other">The amount to subtract.</param>
    /// <returns>The difference.</returns>
    /// <exception cref="InvalidOperationException">The currencies differ.</exception>
    public Money Subtract(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, CurrencyCode);
    }

    /// <summary>Adds two amounts of the same currency.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The sum.</returns>
    /// <exception cref="InvalidOperationException">The currencies differ.</exception>
    public static Money operator +(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.Add(right);
    }

    /// <summary>Subtracts one amount from another of the same currency.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The difference.</returns>
    /// <exception cref="InvalidOperationException">The currencies differ.</exception>
    public static Money operator -(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.Subtract(right);
    }

    /// <summary>
    /// Floors the amount down to the nearest multiple of <paramref name="step"/>
    /// (house-favorable offer rounding, e.g. step <c>5.00</c> turns 8,463.75 into 8,460.00).
    /// The result is re-normalized through <see cref="From"/> so fractional steps (e.g.
    /// <c>0.001</c>) still land on the currency's minor-unit scale.
    /// </summary>
    /// <param name="step">The positive rounding step in major units.</param>
    /// <returns>The floored monetary value, normalized to the currency scale.</returns>
    public Money FloorToNearest(decimal step)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);
        return From(Math.Floor(Amount / step) * step, CurrencyCode);
    }

    /// <summary>Formats the value invariantly for display, e.g. <c>$8,460.00</c> for USD.</summary>
    /// <returns>The display string.</returns>
    public override string ToString() => CurrencyDisplay.Format(this);

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(CurrencyCode, other.CurrencyCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Currency mismatch: cannot combine {CurrencyCode} with {other.CurrencyCode}.");
        }
    }

    private static string NormalizeCurrencyCode(string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        var code = currencyCode.Trim().ToUpperInvariant();
        if (code.Length != 3 || !code.All(char.IsAsciiLetterUpper))
        {
            throw new ArgumentException(
                $"Currency code must be a three-letter ISO 4217 code; got '{currencyCode}'.",
                nameof(currencyCode));
        }

        return code;
    }

    private static decimal Pow10(int digits) => digits switch
    {
        0 => 1m,
        1 => 10m,
        2 => 100m,
        3 => 1000m,
        _ => throw new ArgumentOutOfRangeException(nameof(digits), digits, "Unsupported minor-unit scale."),
    };
}
