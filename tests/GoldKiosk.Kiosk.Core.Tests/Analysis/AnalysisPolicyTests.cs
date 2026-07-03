using FluentAssertions;
using GoldKiosk.Domain.Primitives;
using GoldKiosk.Kiosk.Core.Analysis;
using GoldKiosk.Kiosk.Core.Options;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Core.Tests.Analysis;

[TestFixture]
public sealed class AnalysisPolicyTests
{
    private AnalysisPolicy _policy = null!;

    [SetUp]
    public void CreatePolicy() =>
        _policy = new AnalysisPolicy(new AnalysisOptions
        {
            MinWeightGrams = 1.0m,
            MinGoldPercent = 33.3m,
            VolumeTolerancePercent = 25.0m,
        });

    [Test]
    public void Constructor_NullOptions_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => new AnalysisPolicy(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [TestCase("0")]
    [TestCase("-0.5")]
    public void CheckWeight_NoDetectableWeight_RejectsAsEmptyTray(string weight)
    {
        Result result = _policy.CheckWeight(Parse(weight));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("item.empty_tray");
    }

    [TestCase("0.1")]
    [TestCase("0.999")]
    public void CheckWeight_BelowConfiguredMinimum_RejectsAsUnderweight(string weight)
    {
        Result result = _policy.CheckWeight(Parse(weight));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("item.underweight");
    }

    [TestCase("1.0")]
    [TestCase("12.4")]
    public void CheckWeight_AtOrAboveMinimum_Passes(string weight)
    {
        Result result = _policy.CheckWeight(Parse(weight));

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void CheckComposition_PlatedItem_RejectsAsGoldPlatedEvenWithHighGoldReading()
    {
        Result result = _policy.CheckComposition(91.6m, goldPlated: true);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("item.gold_plated");
    }

    [Test]
    public void CheckComposition_UnreadableGoldContent_RejectsAsUnidentified()
    {
        Result result = _policy.CheckComposition(null, goldPlated: false);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("item.unidentified");
    }

    [Test]
    public void CheckComposition_BelowMinimumPurity_RejectsAsInsufficientPurity()
    {
        Result result = _policy.CheckComposition(33.2m, goldPlated: false);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("item.insufficient_purity");
    }

    [TestCase("33.3")]
    [TestCase("91.6")]
    public void CheckComposition_AtOrAboveMinimumPurity_Passes(string goldPercent)
    {
        Result result = _policy.CheckComposition(Parse(goldPercent), goldPlated: false);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void CheckVolume_UncalibratedChamber_PassesAsAdvisoryOnly()
    {
        var elements = new Dictionary<string, decimal> { ["Au"] = 100m };

        Result result = _policy.CheckVolume(19.32m, elements, 99m, calibrated: false);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void CheckVolume_ZeroWeight_Passes()
    {
        var elements = new Dictionary<string, decimal> { ["Au"] = 100m };

        Result result = _policy.CheckVolume(0m, elements, 5m, calibrated: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void CheckVolume_EmptyComposition_Passes()
    {
        Result result = _policy.CheckVolume(10m, new Dictionary<string, decimal>(), 5m, calibrated: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void CheckVolume_DeviationWithinTolerance_Passes()
    {
        // Pure gold at 19.32 g/cc: 19.32 g computes to exactly 1.00 cc; 1.20 cc measured
        // is a 20% deviation — inside the 25% tolerance.
        var elements = new Dictionary<string, decimal> { ["Au"] = 100m };

        Result result = _policy.CheckVolume(19.32m, elements, 1.20m, calibrated: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void CheckVolume_DeviationBeyondTolerance_RejectsAsUnidentified()
    {
        // 19.32 g of pure gold computes to 1.00 cc; 1.26 cc measured is a 26% deviation.
        var elements = new Dictionary<string, decimal> { ["Au"] = 100m };

        Result result = _policy.CheckVolume(19.32m, elements, 1.26m, calibrated: true);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("item.unidentified");
    }

    [Test]
    public void CheckVolume_MixedComposition_WeighsDensitiesByPercentage()
    {
        // 50% Au (19.32) + 50% Cu (8.96) → 14.14 g/cc; 14.14 g computes to exactly 1.00 cc.
        var elements = new Dictionary<string, decimal> { ["Au"] = 50m, ["Cu"] = 50m };

        Result result = _policy.CheckVolume(14.14m, elements, 1.00m, calibrated: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void CheckVolume_UnknownElement_UsesTheFallbackDensity()
    {
        // Unknown symbols fall back to 8.0 g/cc; 8 g computes to exactly 1.00 cc.
        var elements = new Dictionary<string, decimal> { ["Xx"] = 100m };

        Result result = _policy.CheckVolume(8m, elements, 1.00m, calibrated: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void CheckVolume_NegativePercentages_PassAsAdvisory()
    {
        var elements = new Dictionary<string, decimal> { ["Au"] = -50m };

        Result result = _policy.CheckVolume(10m, elements, 1.00m, calibrated: true);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void CheckVolume_NullElements_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        _policy.Invoking(p => p.CheckVolume(10m, null!, 1m, calibrated: true))
            .Should().Throw<ArgumentNullException>();
    }

    private static decimal Parse(string value) =>
        decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
