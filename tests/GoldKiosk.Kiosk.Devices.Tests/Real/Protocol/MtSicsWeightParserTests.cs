using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Protocol;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Real.Protocol;

[TestFixture]
public sealed class MtSicsWeightParserTests
{
    [Test]
    public void TryParse_StableResponse_ReturnsSettledWeight()
    {
        bool parsed = MtSicsWeightParser.TryParse("S S     12.345 g", out WeightReading reading);

        parsed.Should().BeTrue();
        reading.Should().Be(new WeightReading(12.345m, Stable: true));
    }

    [Test]
    public void TryParse_DynamicResponse_FlagsTheReadingAsUnsettled()
    {
        bool parsed = MtSicsWeightParser.TryParse("S D     12.401 g", out WeightReading reading);

        parsed.Should().BeTrue();
        reading.Should().Be(new WeightReading(12.401m, Stable: false));
    }

    [Test]
    public void TryParse_LegacyQueryResponseWithoutMarker_IsTreatedAsSettled()
    {
        bool parsed = MtSicsWeightParser.TryParse("Q 12.4", out WeightReading reading);

        parsed.Should().BeTrue();
        reading.Stable.Should().BeTrue();
        reading.Grams.Should().Be(12.4m);
    }

    [Test]
    public void TryParse_NegativeWeight_Parses()
    {
        bool parsed = MtSicsWeightParser.TryParse("S S    -0.005 g", out WeightReading reading);

        parsed.Should().BeTrue();
        reading.Grams.Should().Be(-0.005m);
    }

    [TestCase("ES")]
    [TestCase("S I")]
    [TestCase("garbage response")]
    [TestCase("")]
    [TestCase("   ")]
    public void TryParse_ResponseWithoutNumericToken_ReturnsFalse(string response)
    {
        bool parsed = MtSicsWeightParser.TryParse(response, out WeightReading reading);

        parsed.Should().BeFalse();
        reading.Should().Be(new WeightReading(0m, Stable: false));
    }

    [Test]
    public void TryParse_NullResponse_ReturnsFalse()
    {
        bool parsed = MtSicsWeightParser.TryParse(null, out _);

        parsed.Should().BeFalse();
    }

    [Test]
    [SetCulture("de-DE")]
    public void TryParse_UnderCommaDecimalCulture_StillParsesWithInvariantCulture()
    {
        bool parsed = MtSicsWeightParser.TryParse("S S     12.345 g", out WeightReading reading);

        parsed.Should().BeTrue();
        reading.Grams.Should().Be(12.345m, "the legacy machine-culture parse bug must not return");
    }

    [Test]
    public void TryParse_DynamicMarkerAfterTheNumber_DoesNotAffectStability()
    {
        bool parsed = MtSicsWeightParser.TryParse("S 12.4 D", out WeightReading reading);

        parsed.Should().BeTrue();
        reading.Stable.Should().BeTrue("only a 'D' status token before the number marks a settling value");
    }
}
