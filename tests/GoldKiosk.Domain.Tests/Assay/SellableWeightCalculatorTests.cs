using FluentAssertions;
using GoldKiosk.Domain.Assay;
using GoldKiosk.Domain.ValueObjects;
using NUnit.Framework;

namespace GoldKiosk.Domain.Tests.Assay;

[TestFixture]
public sealed class SellableWeightCalculatorTests
{
    [Test]
    public void Compute_StoneLessThanScale_SubtractsStone()
    {
        var sellable = SellableWeightCalculator.Compute(GoldWeight.FromGrams(10m), 2m);

        sellable.Grams.Should().Be(8m);
    }

    [Test]
    public void Compute_StoneExceedsScale_ClampsToZero()
    {
        var sellable = SellableWeightCalculator.Compute(GoldWeight.FromGrams(10m), 12m);

        sellable.Grams.Should().Be(0m);
    }

    [Test]
    public void Compute_NegativeStone_ClampsToScaleWeight()
    {
        var sellable = SellableWeightCalculator.Compute(GoldWeight.FromGrams(10m), -3m);

        sellable.Grams.Should().Be(10m);
    }

    [Test]
    public void Compute_ZeroStone_ReturnsScaleWeight()
    {
        var sellable = SellableWeightCalculator.Compute(GoldWeight.FromGrams(10m), 0m);

        sellable.Grams.Should().Be(10m);
    }

    [Test]
    public void Compute_NullScaleWeight_Throws()
    {
        FluentActions.Invoking(() => SellableWeightCalculator.Compute(null!, 1m))
            .Should().Throw<ArgumentNullException>();
    }
}
