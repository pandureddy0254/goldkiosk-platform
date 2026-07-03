using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Configuration;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Configuration;

[TestFixture]
public sealed class ConnectionOptionsTests
{
    private static readonly ConnectionOptions Fallback = new()
    {
        Port = "COM5",
        BaudRate = 9600,
        Host = "192.168.1.6",
        TcpPort = 29999,
        VendorAssemblyPath = "Vendor.dll",
        Variant = "vanta",
    };

    [Test]
    public void MergedWith_AllMembersConfigured_KeepsEveryConfiguredValue()
    {
        var configured = new ConnectionOptions
        {
            Port = "COM9",
            BaudRate = 115200,
            Host = "10.0.0.5",
            TcpPort = 4000,
            VendorAssemblyPath = "Other.dll",
            Variant = "innovx",
        };

        ConnectionOptions merged = configured.MergedWith(Fallback);

        merged.Should().BeEquivalentTo(configured);
    }

    [Test]
    public void MergedWith_NoMembersConfigured_TakesEveryFallbackValue()
    {
        var configured = new ConnectionOptions();

        ConnectionOptions merged = configured.MergedWith(Fallback);

        merged.Should().BeEquivalentTo(Fallback);
    }

    [Test]
    public void MergedWith_MixedMembers_MergesPerMember()
    {
        var configured = new ConnectionOptions { Port = "COM9", Variant = "innovx" };

        ConnectionOptions merged = configured.MergedWith(Fallback);

        merged.Port.Should().Be("COM9");
        merged.Variant.Should().Be("innovx");
        merged.BaudRate.Should().Be(9600);
        merged.Host.Should().Be("192.168.1.6");
        merged.TcpPort.Should().Be(29999);
        merged.VendorAssemblyPath.Should().Be("Vendor.dll");
    }

    [Test]
    public void MergedWith_DoesNotMutateEitherInstance()
    {
        var configured = new ConnectionOptions { Port = "COM9" };

        configured.MergedWith(Fallback);

        configured.BaudRate.Should().BeNull();
        Fallback.Port.Should().Be("COM5");
    }

    [Test]
    public void MergedWith_NullFallback_Throws()
    {
        var configured = new ConnectionOptions();

        // Null-forgiving: deliberately passing null to exercise the guard clause.
        configured.Invoking(c => c.MergedWith(null!)).Should().Throw<ArgumentNullException>();
    }
}
