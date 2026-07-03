using FluentAssertions;
using GoldKiosk.Domain.Assay;
using NUnit.Framework;

namespace GoldKiosk.Domain.Tests.Assay;

[TestFixture]
public sealed class KaratCalculatorTests
{
    private KaratCalculator _calculator = null!;

    [SetUp]
    public void SetUp() => _calculator = new KaratCalculator(EmbeddedElementMap.Default);

    [Test]
    public void Calculate_DocReferenceSample_MatchesLegacyKaratAndVolume()
    {
        var readings = new ElementReading[]
        {
            new("Au", 92.29m, 0m),
            new("Cu", 5.17m, 0m),
            new("Ag", 2.54m, 0m),
        };

        var result = _calculator.Calculate(readings, 34.04m);

        result.KaratUsingDensity.Should().Be(23.0561m);
        result.KaratUsingXrfPercentage.Should().Be(22.1496m);
        result.CalculatedVolume.Should().Be(1.8340m);
        result.GoldWeightPercent.Should().BeApproximately(96.0673m, 0.001m);
    }

    [Test]
    public void Calculate_SilverDominantSample_ComputesSilverWeight()
    {
        var readings = new ElementReading[]
        {
            new("Ag", 95m, 0m),
            new("Cu", 5m, 0m),
        };

        var result = _calculator.Calculate(readings, 10m);

        result.SilverWeightGrams.Should().BeApproximately(9.5701m, 0.001m);
        result.KaratUsingDensity.Should().Be(0m);
        result.KaratUsingXrfPercentage.Should().Be(0m);
    }

    [Test]
    public void Calculate_NoGoldReading_ReturnsZeroGoldKarat()
    {
        var readings = new ElementReading[]
        {
            new("Ag", 50m, 0m),
            new("Cu", 50m, 0m),
        };

        var result = _calculator.Calculate(readings, 10m);

        result.KaratUsingDensity.Should().Be(0m);
        result.KaratUsingXrfPercentage.Should().Be(0m);
        result.GoldWeightPercent.Should().Be(0m);
    }

    [Test]
    public void Calculate_UnknownElement_ThrowsKeyNotFound()
    {
        var readings = new ElementReading[] { new("Zz", 100m, 0m) };

        FluentActions.Invoking(() => _calculator.Calculate(readings, 10m))
            .Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void Calculate_EmptyReadings_ThrowsArgumentException()
    {
        FluentActions.Invoking(() => _calculator.Calculate([], 10m))
            .Should().Throw<ArgumentException>();
    }

    [Test]
    public void Calculate_NegativeWeight_ThrowsArgumentOutOfRange()
    {
        var readings = new ElementReading[] { new("Au", 90m, 0m) };

        FluentActions.Invoking(() => _calculator.Calculate(readings, -0.01m))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Calculate_AllZeroPercentages_ThrowsInvalidOperation()
    {
        var readings = new ElementReading[] { new("Au", 0m, 0m) };

        FluentActions.Invoking(() => _calculator.Calculate(readings, 10m))
            .Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Default_UsesEmbeddedMap()
    {
        var readings = new ElementReading[] { new("Au", 100m, 0m) };

        var result = KaratCalculator.Default.Calculate(readings, 10m);

        result.KaratUsingXrfPercentage.Should().Be(24m);
    }
}
