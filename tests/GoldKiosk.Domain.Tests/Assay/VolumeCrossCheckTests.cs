using FluentAssertions;
using GoldKiosk.Domain.Assay;
using NUnit.Framework;

namespace GoldKiosk.Domain.Tests.Assay;

[TestFixture]
public sealed class VolumeCrossCheckTests
{
    private static readonly AssayOptions Options = AssayOptions.Default;

    [Test]
    public void Evaluate_ComputesCorrectionFactorAndRatio()
    {
        var result = VolumeCrossCheck.Evaluate(1.834m, 2.0m, 0.5m, Options);

        result.CorrectionFactor.Should().Be(91.7m);
        result.Ratio.Should().Be(0.917m);
        result.IsInRange.Should().BeTrue();
        result.Correction.Should().Be(VolumeCorrection.None);
        result.Rejection.Should().BeNull();
    }

    private static IEnumerable<TestCaseData> InRangeBoundaryCases()
    {
        yield return new TestCaseData(0.65m, 1.0m, true);   // cf = 65 -> in range
        yield return new TestCaseData(0.6499m, 1.0m, false); // cf = 64.99 -> out of range
        yield return new TestCaseData(0.89m, 1.0m, true);   // cf = 89 -> in range
        yield return new TestCaseData(0.90m, 1.0m, true);   // cf = 90 -> in range
    }

    [TestCaseSource(nameof(InRangeBoundaryCases))]
    public void Evaluate_CorrectionFactorBoundary_SetsInRange(decimal calculated, decimal measured, bool expectedInRange)
    {
        var result = VolumeCrossCheck.Evaluate(calculated, measured, 0.5m, Options);

        result.IsInRange.Should().Be(expectedInRange);
    }

    [Test]
    public void Evaluate_HeavyItemInScalingBand_RecommendsElementScaling()
    {
        var result = VolumeCrossCheck.Evaluate(0.8m, 1.0m, 20m, Options);

        result.Correction.Should().Be(VolumeCorrection.HeavyElementScaling);
        result.ElementScaleFactor.Should().Be(0.8m);
        result.AdjustedWeightGrams.Should().Be(20m);
        result.Rejection.Should().BeNull();
    }

    [Test]
    public void Evaluate_HeavyItemAbove89Band_AppliesNoCorrection()
    {
        var result = VolumeCrossCheck.Evaluate(0.95m, 1.0m, 20m, Options);

        result.Correction.Should().Be(VolumeCorrection.None);
        result.Rejection.Should().BeNull();
    }

    [Test]
    public void Evaluate_LightItemInBand_AdjustsWeight()
    {
        var result = VolumeCrossCheck.Evaluate(0.8m, 1.0m, 5m, Options);

        result.Correction.Should().Be(VolumeCorrection.WeightAdjustment);
        result.AdjustedWeightGrams.Should().Be(4.0m);
        result.ElementScaleFactor.Should().Be(1m);
        result.Rejection.Should().BeNull();
    }

    [Test]
    public void Evaluate_HeavyItemOutOfRange_RejectsUnacceptableVolume()
    {
        var result = VolumeCrossCheck.Evaluate(0.6m, 1.2m, 20m, Options);

        result.IsInRange.Should().BeFalse();
        result.Rejection.Should().Be(RejectionReason.UnacceptableVolumeError);
        result.Correction.Should().Be(VolumeCorrection.None);
    }

    [Test]
    public void Evaluate_LightItemOutOfRange_RejectsUnacceptableVolume()
    {
        var result = VolumeCrossCheck.Evaluate(0.6m, 1.2m, 5m, Options);

        result.Rejection.Should().Be(RejectionReason.UnacceptableVolumeError);
        result.Correction.Should().Be(VolumeCorrection.None);
    }

    [Test]
    public void Evaluate_WeightAtRejectThreshold_DoesNotReject()
    {
        var result = VolumeCrossCheck.Evaluate(0.6m, 1.2m, 1m, Options);

        result.Rejection.Should().BeNull();
        result.Correction.Should().Be(VolumeCorrection.None);
    }

    [Test]
    public void Evaluate_MeasuredVolumeNotGreaterThanCalculated_AppliesNoCorrection()
    {
        var result = VolumeCrossCheck.Evaluate(1.0m, 0.9m, 20m, Options);

        result.Correction.Should().Be(VolumeCorrection.None);
        result.Rejection.Should().BeNull();
        result.IsInRange.Should().BeTrue();
    }

    [Test]
    public void Evaluate_CalculatedVolumeBelowThreshold_SkipsFraudCheck()
    {
        var result = VolumeCrossCheck.Evaluate(0.4m, 1.0m, 20m, Options);

        result.IsInRange.Should().BeFalse();
        result.Rejection.Should().BeNull();
        result.Correction.Should().Be(VolumeCorrection.None);
    }

    [Test]
    public void Evaluate_MeasuredVolumeZero_Throws()
    {
        FluentActions.Invoking(() => VolumeCrossCheck.Evaluate(0.5m, 0m, 5m, Options))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Evaluate_NegativeWeight_Throws()
    {
        FluentActions.Invoking(() => VolumeCrossCheck.Evaluate(0.5m, 1.0m, -1m, Options))
            .Should().Throw<ArgumentOutOfRangeException>();
    }
}
