using FluentAssertions;
using GoldKiosk.Domain.ValueObjects;
using NUnit.Framework;

namespace GoldKiosk.Domain.Tests.ValueObjects;

[TestFixture]
public sealed class MoneyTests
{
    [TestCase("2.345", "2.34")]
    [TestCase("2.355", "2.36")]
    [TestCase("2.365", "2.36")]
    [TestCase("-2.345", "-2.34")]
    [TestCase("2.344", "2.34")]
    [TestCase("2.346", "2.35")]
    public void From_UsdAmountWithExtraPrecision_AppliesBankersRounding(string raw, string expected)
    {
        var money = Money.From(decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture), "USD");

        money.Amount.Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [TestCase("1000.5", "1000")]
    [TestCase("1001.5", "1002")]
    public void From_JpyMidpointAmount_RoundsToWholeYenToEven(string raw, string expected)
    {
        var money = Money.From(decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture), "JPY");

        money.Amount.Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Test]
    public void From_BhdAmount_RoundsToThreeMinorDigits()
    {
        var money = Money.From(12.34567m, "BHD");

        money.Amount.Should().Be(12.346m);
    }

    [TestCase(12345L, "USD", "123.45")]
    [TestCase(1000L, "JPY", "1000")]
    [TestCase(12345L, "BHD", "12.345")]
    [TestCase(-12345L, "USD", "-123.45")]
    [TestCase(0L, "USD", "0")]
    public void FromMinorUnits_PerCurrencyScale_ProducesMajorAmount(long minor, string currency, string expected)
    {
        var money = Money.FromMinorUnits(minor, currency);

        money.Amount.Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [TestCase(12345L, "USD")]
    [TestCase(1000L, "JPY")]
    [TestCase(12345L, "BHD")]
    [TestCase(-999L, "USD")]
    public void ToMinorUnits_AfterFromMinorUnits_RoundTripsExactly(long minor, string currency)
    {
        var money = Money.FromMinorUnits(minor, currency);

        money.ToMinorUnits().Should().Be(minor);
    }

    [Test]
    public void ToMinorUnits_WhenScaledAmountExceedsInt64_ThrowsOverflow()
    {
        var money = Money.From(100_000_000_000_000_000m, "USD");

        money.Invoking(m => m.ToMinorUnits()).Should().Throw<OverflowException>();
    }

    [Test]
    public void FloorToNearest_AmountBetweenSteps_FloorsDown()
    {
        var money = Money.From(8463.75m, "USD");

        money.FloorToNearest(5m).Should().Be(Money.From(8460m, "USD"));
    }

    [Test]
    public void FloorToNearest_ExactMultiple_StaysUnchanged()
    {
        var money = Money.From(8460m, "USD");

        money.FloorToNearest(5m).Should().Be(Money.From(8460m, "USD"));
    }

    [Test]
    public void FloorToNearest_NegativeAmount_FloorsTowardNegativeInfinity()
    {
        var money = Money.From(-2.50m, "USD");

        money.FloorToNearest(5m).Should().Be(Money.From(-5m, "USD"));
    }

    [Test]
    public void FloorToNearest_FractionalStep_ReNormalizesToCurrencyScale()
    {
        var money = Money.From(12.34m, "USD");

        Money floored = money.FloorToNearest(0.003m);

        floored.Amount.Should().Be(12.34m, "12.339 must re-normalize onto the USD two-digit scale");
    }

    [TestCase("0")]
    [TestCase("-5")]
    public void FloorToNearest_NonPositiveStep_Throws(string step)
    {
        var money = Money.From(10m, "USD");

        money.Invoking(m => m.FloorToNearest(decimal.Parse(step, System.Globalization.CultureInfo.InvariantCulture)))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Add_SameCurrency_SumsAmounts()
    {
        Money sum = Money.From(1.10m, "USD").Add(Money.From(2.15m, "USD"));

        sum.Should().Be(Money.From(3.25m, "USD"));
    }

    [Test]
    public void Add_DifferentCurrency_ThrowsCurrencyMismatch()
    {
        var usd = Money.From(1m, "USD");
        var eur = Money.From(1m, "EUR");

        usd.Invoking(m => m.Add(eur))
            .Should().Throw<InvalidOperationException>().WithMessage("*USD*EUR*");
    }

    [Test]
    public void Subtract_SameCurrency_ComputesDifference()
    {
        Money difference = Money.From(5m, "USD").Subtract(Money.From(7.25m, "USD"));

        difference.Should().Be(Money.From(-2.25m, "USD"));
    }

    [Test]
    public void Subtract_DifferentCurrency_ThrowsCurrencyMismatch()
    {
        var usd = Money.From(1m, "USD");
        var inr = Money.From(1m, "INR");

        usd.Invoking(m => m.Subtract(inr)).Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void AdditionOperator_SameCurrency_SumsAmounts()
    {
        Money sum = Money.From(2m, "USD") + Money.From(3m, "USD");

        sum.Should().Be(Money.From(5m, "USD"));
    }

    [Test]
    public void SubtractionOperator_SameCurrency_ComputesDifference()
    {
        Money difference = Money.From(5m, "USD") - Money.From(2m, "USD");

        difference.Should().Be(Money.From(3m, "USD"));
    }

    [Test]
    public void Add_NullOther_Throws()
    {
        var money = Money.From(1m, "USD");

        // Null-forgiving: deliberately passing null to exercise the guard clause.
        money.Invoking(m => m.Add(null!)).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Subtract_NullOther_Throws()
    {
        var money = Money.From(1m, "USD");

        // Null-forgiving: deliberately passing null to exercise the guard clause.
        money.Invoking(m => m.Subtract(null!)).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void AdditionOperator_NullLeftOperand_Throws()
    {
        var right = Money.From(1m, "USD");

        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => (Money)null! + right).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void SubtractionOperator_NullLeftOperand_Throws()
    {
        var right = Money.From(1m, "USD");

        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => (Money)null! - right).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void From_LowercaseCurrencyCode_NormalizesToUppercase()
    {
        var money = Money.From(1m, "usd");

        money.CurrencyCode.Should().Be("USD");
    }

    [Test]
    public void From_PaddedCurrencyCode_TrimsBeforeNormalizing()
    {
        var money = Money.From(1m, " jpy ");

        money.CurrencyCode.Should().Be("JPY");
    }

    [TestCase("US")]
    [TestCase("USDX")]
    [TestCase("U1D")]
    [TestCase("U$D")]
    [TestCase("   ")]
    public void From_MalformedCurrencyCode_ThrowsArgumentException(string currencyCode)
    {
        FluentActions.Invoking(() => Money.From(1m, currencyCode)).Should().Throw<ArgumentException>();
    }

    [Test]
    public void From_NullCurrencyCode_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => Money.From(1m, null!)).Should().Throw<ArgumentNullException>();
    }

    [TestCase("8460", "USD", "$8,460.00")]
    [TestCase("1000", "JPY", "¥1,000")]
    [TestCase("12.34", "AED", "AED 12.34")]
    [TestCase("12.345", "BHD", "BHD 12.345")]
    [TestCase("-8460", "USD", "-$8,460.00")]
    [TestCase("-12.34", "AED", "-AED 12.34")]
    [TestCase("1.50", "EUR", "€1.50")]
    [TestCase("2", "GBP", "£2.00")]
    [TestCase("99", "INR", "₹99.00")]
    public void ToString_PerCurrency_FormatsInvariantly(string amount, string currency, string expected)
    {
        var money = Money.From(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture), currency);

        money.ToString().Should().Be(expected);
    }

    [Test]
    public void Equals_SameNormalizedAmountAndCurrency_AreEqual()
    {
        Money.From(5m, "usd").Should().Be(Money.From(5.00m, "USD"));
    }

    [Test]
    public void Equals_SameAmountDifferentCurrency_AreNotEqual()
    {
        Money.From(5m, "USD").Should().NotBe(Money.From(5m, "EUR"));
    }

    [Test]
    public void Equals_DifferentAmountSameCurrency_AreNotEqual()
    {
        Money.From(5m, "USD").Should().NotBe(Money.From(5.01m, "USD"));
    }
}
