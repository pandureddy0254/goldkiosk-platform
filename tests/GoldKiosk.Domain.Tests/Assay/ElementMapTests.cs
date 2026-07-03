using System.Globalization;
using FluentAssertions;
using GoldKiosk.Domain.Assay;
using NUnit.Framework;

namespace GoldKiosk.Domain.Tests.Assay;

[TestFixture]
public sealed class ElementMapTests
{
    [Test]
    public void Parse_ValidContent_LoadsRowsKeyedBySymbol()
    {
        var map = ElementMap.Parse("19.32;Gold;Au;79\n10.5;Silver;Ag;47");

        map.Count.Should().Be(2);
        map.GetDensity("Au").Should().Be(19.32m);
        map.Get("Ag").AtomicNumber.Should().Be(47);
    }

    [Test]
    public void Parse_IgnoresBlankLines()
    {
        var map = ElementMap.Parse("\r\n19.32;Gold;Au;79\n\n   \n10.5;Silver;Ag;47\r\n");

        map.Count.Should().Be(2);
    }

    [Test]
    public void Parse_LineWithWrongFieldCount_ThrowsFormatException()
    {
        FluentActions.Invoking(() => ElementMap.Parse("19.32;Gold;Au"))
            .Should().Throw<FormatException>();
    }

    [Test]
    public void Parse_NonNumericDensity_ThrowsFormatException()
    {
        FluentActions.Invoking(() => ElementMap.Parse("heavy;Gold;Au;79"))
            .Should().Throw<FormatException>();
    }

    [Test]
    public void Parse_NonNumericAtomicNumber_ThrowsFormatException()
    {
        FluentActions.Invoking(() => ElementMap.Parse("19.32;Gold;Au;seventy-nine"))
            .Should().Throw<FormatException>();
    }

    [Test]
    public void Parse_DuplicateSymbol_ThrowsFormatException()
    {
        FluentActions.Invoking(() => ElementMap.Parse("19.32;Gold;Au;79\n19.30;GoldAgain;au;79"))
            .Should().Throw<FormatException>();
    }

    [Test]
    public void Parse_NoRows_ThrowsFormatException()
    {
        FluentActions.Invoking(() => ElementMap.Parse("   \n\n"))
            .Should().Throw<FormatException>();
    }

    [Test]
    public void EmbeddedDefault_HasFullLegacyTable()
    {
        EmbeddedElementMap.Default.Count.Should().Be(97);
    }

    [TestCase("Au", "19.32")]
    [TestCase("Ag", "10.5")]
    [TestCase("Cu", "8.96")]
    [TestCase("W", "19.35")]
    [TestCase("Pt", "21.45")]
    [TestCase("Ir", "22.4")]
    [TestCase("Pd", "12.02")]
    [TestCase("Rh", "12.41")]
    [TestCase("Fe", "7.87")]
    [TestCase("Zn", "7.13")]
    [TestCase("Ni", "8.9")]
    public void EmbeddedDefault_KnownDensities(string symbol, string expectedDensity)
    {
        var expected = decimal.Parse(expectedDensity, CultureInfo.InvariantCulture);

        EmbeddedElementMap.Default.GetDensity(symbol).Should().Be(expected);
    }

    [TestCase("au")]
    [TestCase("AU")]
    [TestCase("Au")]
    public void GetDensity_IsCaseInsensitive(string symbol)
    {
        EmbeddedElementMap.Default.GetDensity(symbol).Should().Be(19.32m);
    }

    [Test]
    public void Contains_KnownAndUnknownSymbol()
    {
        EmbeddedElementMap.Default.Contains("Au").Should().BeTrue();
        EmbeddedElementMap.Default.Contains("Zz").Should().BeFalse();
    }

    [Test]
    public void GetDensity_UnknownSymbol_ThrowsKeyNotFound()
    {
        FluentActions.Invoking(() => EmbeddedElementMap.Default.GetDensity("Zz"))
            .Should().Throw<KeyNotFoundException>();
    }
}
