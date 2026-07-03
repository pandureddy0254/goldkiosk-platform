using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.TestKit;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Contracts;

/// <summary>
/// The <see cref="IScale"/> port contract. Simulators pass it today; real adapters must
/// pass the same fixture against a reference weight on the pan.
/// </summary>
public abstract class ScaleContract
{
    private IScale _scale = null!;

    protected FakeTimeProvider TimeProvider { get; private set; } = null!;

    protected SimulationOptions Simulation { get; private set; } = null!;

    protected abstract decimal ExpectedReferenceGrams { get; }

    protected abstract IScale CreateScale();

    [SetUp]
    public async Task ConnectDeviceAsync()
    {
        TimeProvider = KioskClock.CreateTimeProvider();
        Simulation = new SimulationOptions { LatencyMultiplier = 0 };
        _scale = CreateScale();
        await _scale.ConnectAsync();
    }

    [TearDown]
    public Task DisconnectDeviceAsync() => _scale.DisconnectAsync();

    [Test]
    public async Task GetWeightAsync_ReferenceItemOnPan_ReturnsAStableReading()
    {
        WeightReading reading = await _scale.GetWeightAsync();

        reading.Stable.Should().BeTrue();
        reading.Grams.Should().Be(ExpectedReferenceGrams);
    }

    [Test]
    public async Task GetWeightAsync_AfterReading_LeavesTheDeviceReady()
    {
        await _scale.GetWeightAsync();

        _scale.Health.State.Should().Be(DeviceState.Ready);
    }

    [Test]
    public async Task ZeroAsync_AfterConnect_CompletesAndLeavesTheDeviceReady()
    {
        await _scale.ZeroAsync();

        _scale.Health.State.Should().Be(DeviceState.Ready);
    }

    [Test]
    public void Key_IsTheCanonicalScaleKey()
    {
        _scale.Key.Should().Be(DeviceKeys.Scale);
    }
}
