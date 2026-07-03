using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Protocol.Vanta;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Real.Protocol.Vanta;

[TestFixture]
public sealed class VantaChemistryMapperTests
{
    [Test]
    public void Map_NoFinalResults_FailsWithNoResult()
    {
        AnalysisRun run = VantaChemistryMapper.Map([]);

        run.Succeeded.Should().BeFalse();
        run.FailureReason.Should().Be("analysis_no_result");
    }

    [Test]
    public void Map_EveryResultFailed_FailsWithTheLastStatusId()
    {
        AnalysisRun run = VantaChemistryMapper.Map(
        [
            Result(statusId: 3, ("Au", 91.6, 1.0)),
            Result(statusId: 7, ("Au", 91.6, 1.0)),
        ]);

        run.Succeeded.Should().BeFalse();
        run.FailureReason.Should().Be("analysis_status:7");
    }

    [Test]
    public void Map_MultipleUsableBlobs_AveragesEachElement()
    {
        AnalysisRun run = VantaChemistryMapper.Map(
        [
            Result(statusId: 0, ("Au", 91.0, 1.0), ("Cu", 8.0, 1.0)),
            Result(statusId: 0, ("Au", 92.0, 1.0), ("Cu", 9.0, 1.0)),
        ]);

        run.Succeeded.Should().BeTrue();
        run.GoldPercent.Should().Be(91.5m);
        run.Elements["Cu"].Should().Be(8.5m);
    }

    [Test]
    public void Map_SaturatedBlob_IsExcludedFromTheAverage()
    {
        AnalysisRun run = VantaChemistryMapper.Map(
        [
            Result(statusId: 0, ("Au", 100.0, 15.0)),
            Result(statusId: 0, ("Au", 90.0, 1.0)),
        ]);

        run.Succeeded.Should().BeTrue();
        run.GoldPercent.Should().Be(90m, "the saturated 100%-with-high-error blob is discarded");
    }

    [Test]
    public void Map_SaturatedElementWithLowError_IsKept()
    {
        AnalysisRun run = VantaChemistryMapper.Map(
        [
            Result(statusId: 0, ("Au", 100.0, 14.9)),
        ]);

        run.Succeeded.Should().BeTrue();
        run.GoldPercent.Should().Be(100m);
    }

    [Test]
    public void Map_EveryBlobSaturated_FailsWithNoChemistry()
    {
        AnalysisRun run = VantaChemistryMapper.Map(
        [
            Result(statusId: 0, ("Au", 100.0, 15.0)),
            Result(statusId: 0, ("Ag", 100.0, 20.0)),
        ]);

        run.Succeeded.Should().BeFalse();
        run.FailureReason.Should().Be("analysis_no_chemistry");
    }

    [Test]
    public void Map_SuccessStatusWithEmptyChemistry_FailsWithNoChemistry()
    {
        AnalysisRun run = VantaChemistryMapper.Map([new VantaResult
        {
            Analysis = new VantaAnalysis { StatusId = 0, Final = true },
            Chemistry = [],
        }]);

        run.Succeeded.Should().BeFalse();
        run.FailureReason.Should().Be("analysis_no_chemistry");
    }

    [Test]
    public void Map_MixedFailedAndUsableBlobs_UsesOnlyTheUsableOne()
    {
        AnalysisRun run = VantaChemistryMapper.Map(
        [
            Result(statusId: 5, ("Au", 50.0, 1.0)),
            Result(statusId: 0, ("Au", 91.6, 1.0)),
        ]);

        run.Succeeded.Should().BeTrue();
        run.GoldPercent.Should().Be(91.6m);
    }

    [Test]
    public void Map_ElementSymbols_NormalizeToChemicalCasing()
    {
        AnalysisRun run = VantaChemistryMapper.Map(
        [
            Result(statusId: 0, ("au", 91.6, 1.0), (" AG ", 0.4, 1.0), ("c", 1.0, 1.0)),
        ]);

        run.Elements.Keys.Should().BeEquivalentTo("Au", "Ag", "C");
        run.GoldPercent.Should().Be(91.6m);
        run.SilverPercent.Should().Be(0.4m);
    }

    [Test]
    public void Map_AveragedConcentration_RoundsToFourDecimals()
    {
        AnalysisRun run = VantaChemistryMapper.Map(
        [
            Result(statusId: 0, ("Au", 91.12346, 1.0)),
        ]);

        run.GoldPercent.Should().Be(91.1235m);
    }

    [Test]
    public void Map_NoSilverReading_LeavesSilverNull()
    {
        AnalysisRun run = VantaChemistryMapper.Map(
        [
            Result(statusId: 0, ("Au", 91.6, 1.0)),
        ]);

        run.SilverPercent.Should().BeNull();
    }

    [Test]
    public void Map_BlankElementNames_AreDropped()
    {
        AnalysisRun run = VantaChemistryMapper.Map(
        [
            Result(statusId: 0, ("  ", 5.0, 1.0), ("Au", 91.6, 1.0)),
        ]);

        run.Elements.Keys.Should().BeEquivalentTo("Au");
    }

    [Test]
    public void Map_PlatingIndicatorOnAnyResult_FlagsGoldPlated()
    {
        VantaResult plated = Result(statusId: 0, ("Au", 91.6, 1.0));
        plated = plated with { Analysis = plated.Analysis! with { AuPlating = "Thick" } };

        AnalysisRun run = VantaChemistryMapper.Map([plated]);

        run.GoldPlated.Should().BeTrue();
    }

    [Test]
    public void Map_ExplicitNotPlatedIndicator_LeavesGoldPlatedFalse()
    {
        VantaResult notPlated = Result(statusId: 0, ("Au", 91.6, 1.0));
        notPlated = notPlated with { Analysis = notPlated.Analysis! with { AuPlating = "none" } };

        AnalysisRun run = VantaChemistryMapper.Map([notPlated]);

        run.GoldPlated.Should().BeFalse();
    }

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("   ", false)]
    [TestCase("none", false)]
    [TestCase("No", false)]
    [TestCase("0", false)]
    [TestCase("FALSE", false)]
    [TestCase("NotPlated", false)]
    [TestCase("not plated", false)]
    [TestCase("yes", true)]
    [TestCase("plated", true)]
    [TestCase("Au coating detected", true)]
    public void IndicatesPlating_FreeFormWireValues_ErrCautious(string? auPlating, bool expected)
    {
        VantaChemistryMapper.IndicatesPlating(auPlating).Should().Be(expected);
    }

    [Test]
    public void Map_NullFinalResults_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => VantaChemistryMapper.Map(null!))
            .Should().Throw<ArgumentNullException>();
    }

    private static VantaResult Result(int statusId, params (string Name, double Concentration, double Error)[] elements) =>
        new()
        {
            Analysis = new VantaAnalysis { StatusId = statusId, Final = true },
            Chemistry = [.. elements.Select(e => new VantaElement
            {
                ElementName = e.Name,
                Concentration = e.Concentration,
                Error = e.Error,
            })],
        };
}
