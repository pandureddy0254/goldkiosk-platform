using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Simulation;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Contracts;

[TestFixture]
public sealed class SimulatedMetalAnalyserContractTests : MetalAnalyserContract
{
    protected override IMetalAnalyser CreateAnalyser() => new SimulatedMetalAnalyser(Simulation, TimeProvider);

    [Test]
    public async Task StartAnalysisAsync_Simulator_ReturnsTheCanonicalTwentyTwoKaratResult()
    {
        var analyser = (SimulatedMetalAnalyser)CreateAnalyser();
        await analyser.ConnectAsync();

        AnalysisRun run = await analyser.StartAnalysisAsync();

        run.GoldPercent.Should().Be(91.6m);
        run.SilverPercent.Should().Be(0.4m);
        run.GoldPlated.Should().BeFalse();
        run.Elements.Should().BeEquivalentTo(new Dictionary<string, decimal>
        {
            ["Au"] = 91.6m,
            ["Ag"] = 0.4m,
            ["Cu"] = 8.0m,
        });
    }

    [Test]
    public async Task StartAnalysisAsync_Simulator_EmitsTheScriptedProgressSequence()
    {
        var analyser = (SimulatedMetalAnalyser)CreateAnalyser();
        await analyser.ConnectAsync();
        List<AnalysisProgress> progress = [];
        analyser.ProgressChanged += (_, p) => progress.Add(p);

        await analyser.StartAnalysisAsync();

        progress.Should().Equal(
            new AnalysisProgress(10, "item_detected"),
            new AnalysisProgress(30, "item_detected"),
            new AnalysisProgress(50, "authenticating"),
            new AnalysisProgress(70, "authenticating"),
            new AnalysisProgress(90, "pricing"),
            new AnalysisProgress(100, "pricing"));
    }
}
