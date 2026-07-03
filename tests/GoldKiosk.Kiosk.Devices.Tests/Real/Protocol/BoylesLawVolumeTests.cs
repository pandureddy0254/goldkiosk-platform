using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Real.Protocol;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Real.Protocol;

[TestFixture]
public sealed class BoylesLawVolumeTests
{
    private VolumeChamberOptions _calibration = null!;

    [SetUp]
    public void CreateCalibration() => _calibration = new VolumeChamberOptions();

    [Test]
    public void ComputeItemVolumeCc_PressureDoubles_ReducesToChamberMinusDeltaV()
    {
        // With p2 = 2·p1 and T1 == T2 the denominator is exactly 0.5, so
        // v2 = ΔV/0.5 = 966.9822 and item = 735.0 − (966.9822 − 483.4911) = 251.5089.
        decimal volume = BoylesLawVolume.ComputeItemVolumeCc(14.7m, 29.4m, _calibration);

        volume.Should().Be(251.5089m);
    }

    [Test]
    public void ComputeItemVolumeCc_DifferentialTemperatures_EnterTheDenominator()
    {
        _calibration.Temperature1 = 20;
        _calibration.Temperature2 = 40;
        _calibration.MtLgChamber = 700m;

        // denominator = 1 − (10·40)/(40·20) = 0.5 → v2 = 966.9822 →
        // item = 700 − (966.9822 − 483.4911) = 216.5089.
        decimal volume = BoylesLawVolume.ComputeItemVolumeCc(10m, 40m, _calibration);

        volume.Should().Be(216.5089m);
    }

    [Test]
    public void ComputeItemVolumeCc_EqualPressurePlateaus_ThrowsDegeneratePlateaus()
    {
        FluentActions.Invoking(() => BoylesLawVolume.ComputeItemVolumeCc(20m, 20m, _calibration))
            .Should().Throw<InvalidOperationException>().WithMessage("*plateaus_equal*");
    }

    [TestCase("0", "20")]
    [TestCase("20", "0")]
    [TestCase("-5", "20")]
    [TestCase("20", "-5")]
    public void ComputeItemVolumeCc_NonPositivePressure_Throws(string sealedPsi, string compressedPsi)
    {
        FluentActions.Invoking(() => BoylesLawVolume.ComputeItemVolumeCc(
                Parse(sealedPsi), Parse(compressedPsi), _calibration))
            .Should().Throw<InvalidOperationException>().WithMessage("*not_positive*");
    }

    [Test]
    public void ComputeItemVolumeCc_NullCalibration_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => BoylesLawVolume.ComputeItemVolumeCc(14.7m, 29.4m, null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void IsWithinCalibration_DeltaMatchesTheCalibrationConstantExactly_IsTrue()
    {
        // Default CalDeltaP = 10.0 psi.
        BoylesLawVolume.IsWithinCalibration(14.7m, 24.7m, _calibration).Should().BeTrue();
    }

    [Test]
    public void IsWithinCalibration_DriftEqualToTheTolerance_IsStillTrue()
    {
        // Delta = 11.0 psi, drift 1.0 == default tolerance 1.0 (inclusive gate).
        BoylesLawVolume.IsWithinCalibration(14.7m, 25.7m, _calibration).Should().BeTrue();
    }

    [Test]
    public void IsWithinCalibration_DriftBeyondTheTolerance_IsFalse()
    {
        // Delta = 11.01 psi, drift 1.01 > tolerance 1.0.
        BoylesLawVolume.IsWithinCalibration(14.7m, 25.71m, _calibration).Should().BeFalse();
    }

    [Test]
    public void IsWithinCalibration_NegativeDriftBeyondTheTolerance_IsFalse()
    {
        // Delta = 8.9 psi, drift −1.1 → |drift| > tolerance 1.0.
        BoylesLawVolume.IsWithinCalibration(14.7m, 23.6m, _calibration).Should().BeFalse();
    }

    [Test]
    public void IsWithinCalibration_NullCalibration_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => BoylesLawVolume.IsWithinCalibration(14.7m, 24.7m, null!))
            .Should().Throw<ArgumentNullException>();
    }

    private static decimal Parse(string value) =>
        decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
