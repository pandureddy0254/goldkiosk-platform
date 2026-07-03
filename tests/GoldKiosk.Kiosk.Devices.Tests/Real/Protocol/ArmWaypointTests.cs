using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Real.Protocol;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Real.Protocol;

[TestFixture]
public sealed class ArmWaypointTests
{
    [Test]
    public void Parse_FourComponents_MapsToCartesianPose()
    {
        ArmWaypoint waypoint = ArmWaypoint.Parse("1.5:2:3.25:90", "scale");

        waypoint.Should().Be(new ArmWaypoint(1.5, 2, 3.25, 90));
    }

    [Test]
    public void Parse_FifthLegacyComponent_IsToleratedAndIgnored()
    {
        ArmWaypoint waypoint = ArmWaypoint.Parse("1:2:3:4:99", "tray");

        waypoint.Should().Be(new ArmWaypoint(1, 2, 3, 4));
    }

    [Test]
    public void Parse_NegativeAndPaddedComponents_Parse()
    {
        ArmWaypoint waypoint = ArmWaypoint.Parse(" -10.5 : 0 : 200.75 : -180 ", "home");

        waypoint.Should().Be(new ArmWaypoint(-10.5, 0, 200.75, -180));
    }

    [Test]
    [SetCulture("de-DE")]
    public void Parse_UnderCommaDecimalCulture_UsesInvariantCulture()
    {
        ArmWaypoint waypoint = ArmWaypoint.Parse("1.5:2.5:3.5:4.5", "chamber");

        waypoint.Should().Be(new ArmWaypoint(1.5, 2.5, 3.5, 4.5));
    }

    [TestCase("1:2:3")]
    [TestCase("a:b:c:d")]
    [TestCase("1:2:3:not-a-number")]
    [TestCase(":::")]
    public void Parse_MalformedWaypoint_ThrowsFormatExceptionNamingTheWaypoint(string value)
    {
        FluentActions.Invoking(() => ArmWaypoint.Parse(value, "bag_position"))
            .Should().Throw<FormatException>().WithMessage("arm_waypoint_invalid:bag_position");
    }

    [Test]
    public void Parse_BlankValue_ThrowsArgumentException()
    {
        FluentActions.Invoking(() => ArmWaypoint.Parse(" ", "scale"))
            .Should().Throw<ArgumentException>();
    }

    [Test]
    public void Parse_BlankName_ThrowsArgumentException()
    {
        FluentActions.Invoking(() => ArmWaypoint.Parse("1:2:3:4", " "))
            .Should().Throw<ArgumentException>();
    }
}
