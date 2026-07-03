using FluentAssertions;
using GoldKiosk.Domain.ValueObjects;
using NUnit.Framework;

namespace GoldKiosk.Domain.Tests.ValueObjects;

[TestFixture]
public sealed class GoldWeightTests
{
    [Test]
    public void FromGrams_NegativeWeight_Throws()
    {
        FluentActions.Invoking(() => GoldWeight.FromGrams(-0.001m))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void FromGrams_Zero_IsAllowed()
    {
        var weight = GoldWeight.FromGrams(0m);

        weight.Grams.Should().Be(0m);
    }

    [Test]
    public void ToDwt_OneTroyOunceInGrams_IsExactlyTwentyPennyweight()
    {
        var weight = GoldWeight.FromGrams(31.1034768m);

        weight.ToDwt().Should().Be(20m);
    }

    [Test]
    public void ToDwt_OnePennyweightInGrams_IsExactlyOne()
    {
        var weight = GoldWeight.FromGrams(1.55517384m);

        weight.ToDwt().Should().Be(1m);
    }

    [Test]
    public void ToString_FormatsGramsInvariantly()
    {
        var weight = GoldWeight.FromGrams(12.34m);

        weight.ToString().Should().Be("12.34 g");
    }

    [Test]
    public void Equals_SameGrams_AreEqual()
    {
        GoldWeight.FromGrams(12.4m).Should().Be(GoldWeight.FromGrams(12.4m));
    }
}
