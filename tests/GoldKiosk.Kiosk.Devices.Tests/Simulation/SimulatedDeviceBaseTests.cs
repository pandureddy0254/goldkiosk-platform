using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Exceptions;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Simulation;
using GoldKiosk.TestKit;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Simulation;

[TestFixture]
public sealed class SimulatedDeviceBaseTests
{
    private FakeTimeProvider _timeProvider = null!;
    private SimulationOptions _simulation = null!;

    [SetUp]
    public void CreateOptions()
    {
        _timeProvider = KioskClock.CreateTimeProvider();
        _simulation = new SimulationOptions { LatencyMultiplier = 0 };
    }

    [Test]
    public void Health_BeforeConnect_IsNotInitialized()
    {
        var scale = new SimulatedScale(_simulation, _timeProvider);

        scale.Health.State.Should().Be(DeviceState.NotInitialized);
        scale.Mode.Should().Be(DeviceMode.Mock);
        scale.Key.Should().Be(DeviceKeys.Scale);
        scale.IsCritical.Should().BeTrue();
    }

    [Test]
    public async Task ConnectAsync_TransitionsThroughConnectingToReady()
    {
        var scale = new SimulatedScale(_simulation, _timeProvider);
        List<DeviceState> observed = [];
        scale.HealthChanged += (_, health) => observed.Add(health.State);

        await scale.ConnectAsync();

        observed.Should().Equal(DeviceState.Connecting, DeviceState.Ready);
        scale.Health.State.Should().Be(DeviceState.Ready);
    }

    [Test]
    public async Task DisconnectAsync_MovesToDisconnected()
    {
        var scale = new SimulatedScale(_simulation, _timeProvider);
        await scale.ConnectAsync();

        await scale.DisconnectAsync();

        scale.Health.State.Should().Be(DeviceState.Disconnected);
    }

    [Test]
    public async Task HealthChanged_RepeatedIdenticalState_RaisesOnlyOnChange()
    {
        var scale = new SimulatedScale(_simulation, _timeProvider);
        await scale.ConnectAsync();
        List<DeviceState> observed = [];
        scale.HealthChanged += (_, health) => observed.Add(health.State);

        await scale.DisconnectAsync();
        await scale.DisconnectAsync();

        observed.Should().Equal(DeviceState.Disconnected);
    }

    [Test]
    public async Task ConnectAsync_FaultInjectedDevice_StillConnects()
    {
        _simulation.FaultDevices.Add("scale");
        var scale = new SimulatedScale(_simulation, _timeProvider);

        await scale.ConnectAsync();

        scale.Health.State.Should().Be(DeviceState.Ready);
    }

    [Test]
    public async Task GetWeightAsync_FaultInjectedDevice_ThrowsAndMarksTheDeviceFaulted()
    {
        _simulation.FaultDevices.Add("scale");
        var scale = new SimulatedScale(_simulation, _timeProvider);
        await scale.ConnectAsync();

        Func<Task> operation = () => scale.GetWeightAsync();

        (await operation.Should().ThrowAsync<SimulatedDeviceFaultException>())
            .Which.DeviceKey.Should().Be("scale");
        scale.Health.State.Should().Be(DeviceState.Faulted);
    }

    [Test]
    public async Task GetWeightAsync_FaultKeyInDifferentCase_StillInjectsTheFault()
    {
        _simulation.FaultDevices.Add("SCALE");
        var scale = new SimulatedScale(_simulation, _timeProvider);
        await scale.ConnectAsync();

        await scale.Awaiting(s => s.GetWeightAsync())
            .Should().ThrowAsync<SimulatedDeviceFaultException>();
    }

    [Test]
    public async Task ProbeAsync_FaultInjectedDevice_ReportsFailureWithoutThrowing()
    {
        _simulation.FaultDevices.Add("scale");
        var scale = new SimulatedScale(_simulation, _timeProvider);
        await scale.ConnectAsync();

        DeviceProbeResult probe = await scale.ProbeAsync();

        probe.Passed.Should().BeFalse();
        probe.Detail.Should().Contain("scale");
    }

    [Test]
    public async Task ProbeAsync_HealthyDevice_Passes()
    {
        var scale = new SimulatedScale(_simulation, _timeProvider);
        await scale.ConnectAsync();

        DeviceProbeResult probe = await scale.ProbeAsync();

        probe.Passed.Should().BeTrue();
    }

    [Test]
    public async Task GetWeightAsync_ZeroLatencyMultiplier_CompletesWithoutAdvancingTheFakeClock()
    {
        var scale = new SimulatedScale(_simulation, _timeProvider);
        await scale.ConnectAsync();

        WeightReading reading = await scale.GetWeightAsync();

        reading.Grams.Should().Be(12.4m);
    }

    [Test]
    public async Task GetWeightAsync_UnitLatencyMultiplier_WaitsOnTheInjectedTimeProvider()
    {
        _simulation.LatencyMultiplier = 1.0;
        var scale = new SimulatedScale(_simulation, _timeProvider);
        Task connect = scale.ConnectAsync();
        _timeProvider.Advance(TimeSpan.FromMilliseconds(150));
        await connect;

        Task<WeightReading> pending = scale.GetWeightAsync();

        pending.IsCompleted.Should().BeFalse("the simulated 400 ms read must wait on the fake clock");
        _timeProvider.Advance(TimeSpan.FromMilliseconds(400));
        (await pending).Stable.Should().BeTrue();
    }
}
