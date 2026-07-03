using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.TestKit;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Contracts;

/// <summary>
/// The <see cref="IMetalAnalyser"/> port contract: progress advances monotonically to 100
/// through the UI checklist stages, the device is Busy during the run and Ready after,
/// and a reference item produces a usable gold composition.
/// </summary>
public abstract class MetalAnalyserContract
{
    private static readonly string[] KnownStages = ["item_detected", "authenticating", "pricing"];

    private IMetalAnalyser _analyser = null!;

    protected FakeTimeProvider TimeProvider { get; private set; } = null!;

    protected SimulationOptions Simulation { get; private set; } = null!;

    protected abstract IMetalAnalyser CreateAnalyser();

    [SetUp]
    public async Task ConnectDeviceAsync()
    {
        TimeProvider = KioskClock.CreateTimeProvider();
        Simulation = new SimulationOptions { LatencyMultiplier = 0 };
        _analyser = CreateAnalyser();
        await _analyser.ConnectAsync();
    }

    [TearDown]
    public Task DisconnectDeviceAsync() => _analyser.DisconnectAsync();

    [Test]
    public async Task StartAnalysisAsync_ReferenceItem_ProducesAUsableGoldComposition()
    {
        AnalysisRun run = await _analyser.StartAnalysisAsync();

        run.Succeeded.Should().BeTrue();
        run.GoldPercent.Should().NotBeNull();
        run.Elements.Should().ContainKey("Au");
    }

    [Test]
    public async Task StartAnalysisAsync_ProgressPercents_AdvanceMonotonicallyToOneHundred()
    {
        List<AnalysisProgress> progress = [];
        _analyser.ProgressChanged += (_, p) => progress.Add(p);

        await _analyser.StartAnalysisAsync();

        progress.Should().NotBeEmpty();
        progress.Select(p => p.Percent).Should().BeInAscendingOrder();
        progress[^1].Percent.Should().Be(100);
    }

    [Test]
    public async Task StartAnalysisAsync_ProgressStages_AreTheUiChecklistStages()
    {
        List<AnalysisProgress> progress = [];
        _analyser.ProgressChanged += (_, p) => progress.Add(p);

        await _analyser.StartAnalysisAsync();

        progress.Select(p => p.Stage).Should().OnlyContain(stage => KnownStages.Contains(stage));
    }

    [Test]
    public async Task StartAnalysisAsync_HealthTransitions_AreBusyDuringAndReadyAfter()
    {
        List<DeviceState> observed = [];
        _analyser.HealthChanged += (_, health) => observed.Add(health.State);

        await _analyser.StartAnalysisAsync();

        observed.Should().Equal(DeviceState.Busy, DeviceState.Ready);
        _analyser.Health.State.Should().Be(DeviceState.Ready);
    }

    [Test]
    public void Key_IsTheCanonicalMetalAnalyserKey()
    {
        _analyser.Key.Should().Be(DeviceKeys.MetalAnalyser);
    }
}
