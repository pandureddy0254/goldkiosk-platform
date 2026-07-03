using FluentAssertions;
using GoldKiosk.Domain.ValueObjects;
using NUnit.Framework;

namespace GoldKiosk.Domain.Tests.ValueObjects;

[TestFixture]
public sealed class PurityTests
{
    [TestCase("18", "750")]
    [TestCase("24", "1000")]
    [TestCase("12", "500")]
    [TestCase("21.6", "900")]
    public void FromKarat_KnownValues_ConvertsToFineness(string karat, string expectedFineness)
    {
        var purity = Purity.FromKarat(Parse(karat));

        purity.Fineness.Should().Be(Parse(expectedFineness));
    }

    [Test]
    public void FromKarat_LowerBoundInclusive_Succeeds()
    {
        var purity = Purity.FromKarat(8m);

        purity.Karat.Should().Be(8m);
    }

    [Test]
    public void FromKarat_UpperBoundInclusive_Succeeds()
    {
        var purity = Purity.FromKarat(24m);

        purity.Karat.Should().Be(24m);
    }

    [TestCase("7.999")]
    [TestCase("24.001")]
    [TestCase("0")]
    [TestCase("-1")]
    public void FromKarat_OutsideBounds_Throws(string karat)
    {
        FluentActions.Invoking(() => Purity.FromKarat(Parse(karat)))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void FromFineness_750_ConvertsToEighteenKarat()
    {
        var purity = Purity.FromFineness(750m);

        purity.Karat.Should().Be(18m);
    }

    [Test]
    public void FromFineness_ThenFineness_RoundTripsExactly()
    {
        var purity = Purity.FromFineness(916.7m);

        purity.Fineness.Should().Be(916.7m);
    }

    [Test]
    public void FromFineness_LowerBoundInclusive_PreservesExactKarat()
    {
        var purity = Purity.FromFineness(333m);

        purity.Karat.Should().Be(7.992m, "the nominal 8-karat stamp's exact conversion is preserved");
    }

    [Test]
    public void FromFineness_UpperBoundInclusive_IsPureMetal()
    {
        var purity = Purity.FromFineness(1000m);

        purity.Karat.Should().Be(24m);
    }

    [TestCase("332.999")]
    [TestCase("1000.001")]
    [TestCase("0")]
    [TestCase("-750")]
    public void FromFineness_OutsideBounds_Throws(string fineness)
    {
        FluentActions.Invoking(() => Purity.FromFineness(Parse(fineness)))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Equals_SamePurityFromEitherFactory_AreEqual()
    {
        Purity.FromKarat(18m).Should().Be(Purity.FromFineness(750m));
    }

    private static decimal Parse(string value) =>
        decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
