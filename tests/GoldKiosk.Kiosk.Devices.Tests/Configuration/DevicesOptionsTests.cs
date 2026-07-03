using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Configuration;

[TestFixture]
public sealed class DevicesOptionsTests
{
    [TestCase("Real", DeviceMode.Real)]
    [TestCase("Mock", DeviceMode.Mock)]
    [TestCase("real", DeviceMode.Real)]
    [TestCase("MOCK", DeviceMode.Mock)]
    public void ResolveMode_WithoutOverride_UsesTheDefaultModeCaseInsensitively(string configured, DeviceMode expected)
    {
        var options = new DevicesOptions { DefaultMode = configured };

        options.ResolveMode(DeviceKeys.Scale).Should().Be(expected);
    }

    [TestCase("cash_dispenser")]
    [TestCase("CASH_DISPENSER")]
    [TestCase("CashDispenser")]
    [TestCase("cashdispenser")]
    public void ResolveMode_OverrideKey_MatchesCaseAndUnderscoreInsensitively(string overrideKey)
    {
        var options = new DevicesOptions
        {
            DefaultMode = "Real",
            Overrides = { [overrideKey] = new DeviceOverride { Mode = "Mock" } },
        };

        options.ResolveMode(DeviceKeys.CashDispenser).Should().Be(DeviceMode.Mock);
    }

    [TestCase("scale")]
    [TestCase("SCALE")]
    [TestCase("Scale")]
    public void ResolveMode_LookupKey_MatchesTheOverrideCaseInsensitively(string lookupKey)
    {
        var options = new DevicesOptions
        {
            DefaultMode = "Real",
            Overrides = { ["scale"] = new DeviceOverride { Mode = "Mock" } },
        };

        options.ResolveMode(lookupKey).Should().Be(DeviceMode.Mock);
    }

    [Test]
    public void ResolveMode_OverrideForAnotherDevice_LeavesThisDeviceOnTheDefault()
    {
        var options = new DevicesOptions
        {
            DefaultMode = "Real",
            Overrides = { ["tray"] = new DeviceOverride { Mode = "Mock" } },
        };

        options.ResolveMode(DeviceKeys.Scale).Should().Be(DeviceMode.Real);
    }

    [Test]
    public void ResolveMode_InvalidDefaultMode_ThrowsWithTheOffendingValue()
    {
        var options = new DevicesOptions { DefaultMode = "Banana" };

        options.Invoking(o => o.ResolveMode(DeviceKeys.Scale))
            .Should().Throw<InvalidOperationException>().WithMessage("*Banana*");
    }

    [Test]
    public void ResolveMode_InvalidOverrideMode_Throws()
    {
        var options = new DevicesOptions
        {
            DefaultMode = "Real",
            Overrides = { ["scale"] = new DeviceOverride { Mode = "Simulated" } },
        };

        options.Invoking(o => o.ResolveMode(DeviceKeys.Scale))
            .Should().Throw<InvalidOperationException>().WithMessage("*Simulated*");
    }

    [Test]
    public void ResolveMode_BlankKey_Throws()
    {
        var options = new DevicesOptions();

        options.Invoking(o => o.ResolveMode(" ")).Should().Throw<ArgumentException>();
    }

    [Test]
    public void ResolveConnection_WithoutOverride_ReturnsTheLegacyParityDefaults()
    {
        var options = new DevicesOptions();

        ConnectionOptions connection = options.ResolveConnection(DeviceKeys.Scale);

        connection.Port.Should().Be("COM5");
        connection.BaudRate.Should().Be(9600);
    }

    [Test]
    public void ResolveConnection_PartialOverride_WinsPerMemberOverTheDefaults()
    {
        var options = new DevicesOptions
        {
            Overrides =
            {
                ["scale"] = new DeviceOverride
                {
                    Mode = "Real",
                    Connection = new ConnectionOptions { Port = "COM9" },
                },
            },
        };

        ConnectionOptions connection = options.ResolveConnection(DeviceKeys.Scale);

        connection.Port.Should().Be("COM9", "the configured member wins");
        connection.BaudRate.Should().Be(9600, "unset members fall back to the legacy default");
    }

    [Test]
    public void ResolveConnection_OverrideKey_MatchesCaseAndUnderscoreInsensitively()
    {
        var options = new DevicesOptions
        {
            Overrides =
            {
                ["MetalAnalyser"] = new DeviceOverride
                {
                    Mode = "Real",
                    Connection = new ConnectionOptions { Variant = "innovx" },
                },
            },
        };

        ConnectionOptions connection = options.ResolveConnection(DeviceKeys.MetalAnalyser);

        connection.Variant.Should().Be("innovx");
        connection.Host.Should().Be("192.168.7.2");
        connection.TcpPort.Should().Be(7860);
    }

    [Test]
    public void ResolveConnection_UnknownDeviceKey_ReturnsEmptyOptions()
    {
        var options = new DevicesOptions();

        ConnectionOptions connection = options.ResolveConnection("teleporter");

        connection.Should().BeEquivalentTo(new ConnectionOptions());
    }

    [Test]
    public void ResolveConnection_BlankKey_Throws()
    {
        var options = new DevicesOptions();

        options.Invoking(o => o.ResolveConnection(string.Empty)).Should().Throw<ArgumentException>();
    }
}
