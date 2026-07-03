using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.TestKit;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Contracts;

/// <summary>
/// The <see cref="ITray"/> port contract: the tray starts closed, reports
/// <see cref="TrayState.Moving"/> during each commanded motion and lands on the commanded
/// end state. Runs at unit latency against the injected fake clock so the in-motion state
/// is observable.
/// </summary>
public abstract class TrayContract
{
    private ITray _tray = null!;

    protected FakeTimeProvider TimeProvider { get; private set; } = null!;

    protected SimulationOptions Simulation { get; private set; } = null!;

    /// <summary>The fake-clock advance that completes one commanded tray motion.</summary>
    protected virtual TimeSpan MotionDuration => TimeSpan.FromMilliseconds(1500);

    protected abstract ITray CreateTray();

    [SetUp]
    public async Task ConnectDeviceAsync()
    {
        TimeProvider = KioskClock.CreateTimeProvider();
        Simulation = new SimulationOptions { LatencyMultiplier = 1.0 };
        _tray = CreateTray();
        Task connect = _tray.ConnectAsync();
        TimeProvider.Advance(TimeSpan.FromMilliseconds(150));
        await connect;
    }

    [TearDown]
    public Task DisconnectDeviceAsync() => _tray.DisconnectAsync();

    [Test]
    public void State_AfterConnect_IsClosed()
    {
        _tray.State.Should().Be(TrayState.Closed);
    }

    [Test]
    public async Task OpenAsync_PassesThroughMovingAndLandsOpen()
    {
        Task motion = _tray.OpenAsync();

        _tray.State.Should().Be(TrayState.Moving, "the drawer is in motion until it reports fully open");
        TimeProvider.Advance(MotionDuration);
        await motion;
        _tray.State.Should().Be(TrayState.Open);
    }

    [Test]
    public async Task CloseAsync_AfterOpening_PassesThroughMovingAndLandsClosed()
    {
        Task opening = _tray.OpenAsync();
        TimeProvider.Advance(MotionDuration);
        await opening;

        Task closing = _tray.CloseAsync();

        _tray.State.Should().Be(TrayState.Moving);
        TimeProvider.Advance(MotionDuration);
        await closing;
        _tray.State.Should().Be(TrayState.Closed);
    }
}
