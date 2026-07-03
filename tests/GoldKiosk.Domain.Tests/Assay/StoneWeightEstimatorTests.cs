using FluentAssertions;
using GoldKiosk.Domain.Assay;
using NUnit.Framework;

namespace GoldKiosk.Domain.Tests.Assay;

[TestFixture]
public sealed class StoneWeightEstimatorTests
{
    private static readonly StoneCalibration LinearSquare =
        new(StoneType.Glass, Alpha: 1.0d, Beta: 2.0d, DensityGramsPerCc: 2.5m);

    [Test]
    public void Estimate_PowerLaw_ComputesPerStoneAndTotalGrams()
    {
        var regions = new StoneRegion[] { new(2.0d), new(3.0d) };

        var estimate = StoneWeightEstimator.Estimate(regions, LinearSquare);

        // carats = alpha * area^beta = {4, 9}; grams = carats * 0.2 = {0.8, 1.8}.
        estimate.PerStoneGrams.Should().Equal(0.8m, 1.8m);
        estimate.TotalGrams.Should().Be(2.6m);
    }

    [Test]
    public void Estimate_EmptyRegions_ReturnsZero()
    {
        var estimate = StoneWeightEstimator.Estimate([], LinearSquare);

        estimate.TotalGrams.Should().Be(0m);
        estimate.PerStoneGrams.Should().BeEmpty();
    }

    [Test]
    public void Estimate_AlwaysReportOnlyConfidence()
    {
        var estimate = StoneWeightEstimator.Estimate([new StoneRegion(5.0d)], LinearSquare);

        estimate.Confidence.Should().Be(StoneConfidence.ReportOnly);
    }

    [Test]
    public void Estimate_DeclaredTypeMatchesCalibration_Succeeds()
    {
        var estimate = StoneWeightEstimator.Estimate([new StoneRegion(2.0d)], LinearSquare, StoneType.Glass);

        estimate.PerStoneGrams.Should().Equal(0.8m);
    }

    [Test]
    public void Estimate_DeclaredTypeMismatch_Throws()
    {
        FluentActions.Invoking(() => StoneWeightEstimator.Estimate([new StoneRegion(2.0d)], LinearSquare, StoneType.Ruby))
            .Should().Throw<ArgumentException>();
    }

    [Test]
    public void Estimate_NegativeArea_Throws()
    {
        FluentActions.Invoking(() => StoneWeightEstimator.Estimate([new StoneRegion(-1.0d)], LinearSquare))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void DiamondEquivalent_HasDocumentedDefaultConstants()
    {
        var calibration = StoneCalibration.DiamondEquivalent;

        calibration.StoneType.Should().Be(StoneType.DiamondEquivalent);
        calibration.Alpha.Should().Be(0.0052d);
        calibration.Beta.Should().Be(1.5d);
        calibration.DensityGramsPerCc.Should().Be(3.52m);
        StoneCalibration.GramsPerCarat.Should().Be(0.2m);
    }
}
