using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.TestKit;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Contracts;

/// <summary>
/// The <see cref="IIdScanner"/> port contract against a reference adult, unexpired
/// government document: the scan succeeds and every eligibility-relevant field is usable.
/// </summary>
public abstract class IdScannerContract
{
    private IIdScanner _scanner = null!;

    protected FakeTimeProvider TimeProvider { get; private set; } = null!;

    protected SimulationOptions Simulation { get; private set; } = null!;

    protected abstract IIdScanner CreateScanner();

    [SetUp]
    public async Task ConnectDeviceAsync()
    {
        TimeProvider = KioskClock.CreateTimeProvider();
        Simulation = new SimulationOptions { LatencyMultiplier = 0 };
        _scanner = CreateScanner();
        await _scanner.ConnectAsync();
    }

    [TearDown]
    public Task DisconnectDeviceAsync() => _scanner.DisconnectAsync();

    [Test]
    public async Task ScanAsync_ReferenceDocument_SucceedsWithADocument()
    {
        IdScanResult result = await _scanner.ScanAsync();

        result.Succeeded.Should().BeTrue();
        result.FailureReason.Should().BeNull();
        result.Document.Should().NotBeNull();
    }

    [Test]
    public async Task ScanAsync_ReferenceDocument_IsAGovernmentIdForAnAdult()
    {
        DateOnly today = DateOnly.FromDateTime(TimeProvider.GetUtcNow().UtcDateTime);

        IdScanResult result = await _scanner.ScanAsync();

        result.Document!.IsGovernmentId.Should().BeTrue();
        result.Document.DateOfBirth.Should().BeOnOrBefore(today.AddYears(-18), "the reference holder is an adult");
        result.Document.ExpiresOn.Should().BeOnOrAfter(today, "the reference document is unexpired");
    }

    [Test]
    public async Task ScanAsync_ReferenceDocument_CarriesNamesNumberAndPortrait()
    {
        IdScanResult result = await _scanner.ScanAsync();

        result.Document!.FirstName.Should().NotBeNullOrWhiteSpace();
        result.Document.LastName.Should().NotBeNullOrWhiteSpace();
        result.Document.DocumentNumber.Should().NotBeNullOrWhiteSpace();
        result.Document.PortraitImage.Should().NotBeEmpty();
    }
}
