using FluentAssertions;
using GoldKiosk.Domain.ValueObjects;
using NUnit.Framework;

namespace GoldKiosk.Domain.Tests.ValueObjects;

[TestFixture]
public sealed class CurrencyDisplayTests
{
    [TestCase("BHD", 3)]
    [TestCase("IQD", 3)]
    [TestCase("JOD", 3)]
    [TestCase("KWD", 3)]
    [TestCase("LYD", 3)]
    [TestCase("OMR", 3)]
    [TestCase("TND", 3)]
    [TestCase("JPY", 0)]
    [TestCase("KRW", 0)]
    [TestCase("VND", 0)]
    [TestCase("USD", 2)]
    [TestCase("EUR", 2)]
    public void GetMinorUnitDigits_KnownCurrencies_MatchIso4217Exponents(string currency, int expected)
    {
        CurrencyDisplay.GetMinorUnitDigits(currency).Should().Be(expected);
    }

    [Test]
    public void GetMinorUnitDigits_UnknownCurrency_DefaultsToTwo()
    {
        CurrencyDisplay.GetMinorUnitDigits("AED").Should().Be(2);
    }

    [Test]
    public void GetMinorUnitDigits_WhitespaceCode_Throws()
    {
        FluentActions.Invoking(() => CurrencyDisplay.GetMinorUnitDigits(" "))
            .Should().Throw<ArgumentException>();
    }

    [Test]
    public void Format_NullMoney_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => CurrencyDisplay.Format(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Format_SymbolCurrency_UsesSymbolWithGroupSeparators()
    {
        CurrencyDisplay.Format(Money.From(1234567.89m, "USD")).Should().Be("$1,234,567.89");
    }

    [Test]
    public void Format_SymbollessCurrency_PrefixesTheCode()
    {
        CurrencyDisplay.Format(Money.From(0.5m, "SGD")).Should().Be("SGD 0.50");
    }

    [Test]
    public void Format_NegativeSymbolless_PlacesSignBeforeCode()
    {
        CurrencyDisplay.Format(Money.From(-3.10m, "AED")).Should().Be("-AED 3.10");
    }
}
