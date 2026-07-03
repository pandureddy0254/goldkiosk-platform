using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.TestKit;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Contracts;

/// <summary>
/// The <see cref="IBagger"/> port contract: idle before any cycle, <c>Bagging</c> while a
/// cycle runs (the new-transaction interlock) and idle again once the item is sealed.
/// Runs at unit latency against the injected fake clock so the in-cycle status is observable.
/// </summary>
public abstract class BaggerContract
{
    private IBagger _bagger = null!;

    protected FakeTimeProvider TimeProvider { get; private set; } = null!;

    protected SimulationOptions Simulation { get; private set; } = null!;

    /// <summary>The fake-clock advance that completes one status read.</summary>
    protected virtual TimeSpan StatusReadDuration => TimeSpan.FromMilliseconds(50);

    /// <summary>The fake-clock advance that completes one full bag cycle.</summary>
    protected virtual TimeSpan BagCycleDuration => TimeSpan.FromMilliseconds(3000);

    protected abstract IBagger CreateBagger();

    [SetUp]
    public async Task ConnectDeviceAsync()
    {
        TimeProvider = KioskClock.CreateTimeProvider();
        Simulation = new SimulationOptions { LatencyMultiplier = 1.0 };
        _bagger = CreateBagger();
        Task connect = _bagger.ConnectAsync();
        TimeProvider.Advance(TimeSpan.FromMilliseconds(150));
        await connect;
    }

    [TearDown]
    public Task DisconnectDeviceAsync() => _bagger.DisconnectAsync();

    [Test]
    public async Task GetStatusAsync_BeforeAnyCycle_IsIdle()
    {
        Task<BaggerStatus> status = _bagger.GetStatusAsync();

        TimeProvider.Advance(StatusReadDuration);

        (await status).Should().Be(BaggerStatus.Idle);
    }

    [Test]
    public async Task GetStatusAsync_WhileBagCycleRuns_ReportsBaggingForTheInterlock()
    {
        Task cycle = _bagger.BagItemAsync("BAG-042");
        Task<BaggerStatus> during = _bagger.GetStatusAsync();
        TimeProvider.Advance(StatusReadDuration);

        (await during).Should().Be(BaggerStatus.Bagging);

        TimeProvider.Advance(BagCycleDuration - StatusReadDuration);
        await cycle;
    }

    [Test]
    public async Task GetStatusAsync_AfterTheCycleCompletes_ReturnsToIdle()
    {
        Task cycle = _bagger.BagItemAsync("BAG-042");
        TimeProvider.Advance(BagCycleDuration);
        await cycle;

        Task<BaggerStatus> after = _bagger.GetStatusAsync();
        TimeProvider.Advance(StatusReadDuration);

        (await after).Should().Be(BaggerStatus.Idle);
    }

    [Test]
    public async Task BagItemAsync_BlankBagNumber_Throws()
    {
        await _bagger.Awaiting(b => b.BagItemAsync(" "))
            .Should().ThrowAsync<ArgumentException>();
    }
}
