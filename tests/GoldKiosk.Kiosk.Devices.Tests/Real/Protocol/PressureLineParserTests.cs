using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Real.Protocol;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Real.Protocol;

[TestFixture]
public sealed class PressureLineParserTests
{
    [Test]
    public void TryParse_BracketedSensorLine_ExtractsThePressure()
    {
        bool parsed = PressureLineParser.TryParse(">  4.9812 psi<", out decimal pressure);

        parsed.Should().BeTrue();
        pressure.Should().Be(4.9812m);
    }

    [Test]
    public void TryParse_LineWithCrLfNoise_ScrubsBeforeParsing()
    {
        bool parsed = PressureLineParser.TryParse("\r\n> 14.7 psi <\r\n", out decimal pressure);

        parsed.Should().BeTrue();
        pressure.Should().Be(14.7m);
    }

    [Test]
    public void TryParse_BareNumber_Parses()
    {
        bool parsed = PressureLineParser.TryParse("10.5", out decimal pressure);

        parsed.Should().BeTrue();
        pressure.Should().Be(10.5m);
    }

    [Test]
    public void TryParse_FirstTokenNotNumeric_ReturnsFalse()
    {
        bool parsed = PressureLineParser.TryParse("psi 4.98", out decimal pressure);

        parsed.Should().BeFalse();
        pressure.Should().Be(0m);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("><")]
    public void TryParse_EmptyOrScrubbedToNothing_ReturnsFalse(string line)
    {
        bool parsed = PressureLineParser.TryParse(line, out _);

        parsed.Should().BeFalse();
    }

    [Test]
    public void TryParse_NullLine_ReturnsFalse()
    {
        bool parsed = PressureLineParser.TryParse(null, out _);

        parsed.Should().BeFalse();
    }

    [Test]
    [SetCulture("de-DE")]
    public void TryParse_UnderCommaDecimalCulture_StillParsesWithInvariantCulture()
    {
        bool parsed = PressureLineParser.TryParse("> 4.9812 psi<", out decimal pressure);

        parsed.Should().BeTrue();
        pressure.Should().Be(4.9812m);
    }
}
