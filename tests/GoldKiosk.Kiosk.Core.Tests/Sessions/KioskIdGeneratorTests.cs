using FluentAssertions;
using GoldKiosk.Kiosk.Core.Sessions;
using GoldKiosk.TestKit;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Core.Tests.Sessions;

[TestFixture]
public sealed class KioskIdGeneratorTests
{
    private const string CrockfordAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    [Test]
    public void NewSessionId_HasThePrefixAndUlidLength()
    {
        string id = KioskIdGenerator.NewSessionId(KioskClock.CreateTimeProvider());

        id.Should().StartWith("ses_");
        id.Should().HaveLength(30);
    }

    [Test]
    public void NewOfferId_HasThePrefixAndUlidLength()
    {
        string id = KioskIdGenerator.NewOfferId(KioskClock.CreateTimeProvider());

        id.Should().StartWith("off_");
        id.Should().HaveLength(30);
    }

    [Test]
    public void NewReceiptId_HasThePrefixAndUlidLength()
    {
        string id = KioskIdGenerator.NewReceiptId(KioskClock.CreateTimeProvider());

        id.Should().StartWith("rcp_");
        id.Should().HaveLength(30);
    }

    [Test]
    public void NewSessionId_UsesOnlyCrockfordBase32Characters()
    {
        string id = KioskIdGenerator.NewSessionId(KioskClock.CreateTimeProvider());

        id["ses_".Length..].Should().MatchRegex($"^[{CrockfordAlphabet}]{{26}}$");
    }

    [Test]
    public void NewSessionId_SameTimestamp_ProducesDistinctIds()
    {
        var timeProvider = KioskClock.CreateTimeProvider();

        string first = KioskIdGenerator.NewSessionId(timeProvider);
        string second = KioskIdGenerator.NewSessionId(timeProvider);

        first.Should().NotBe(second);
    }

    [Test]
    public void NewSessionId_LaterTimestamp_SortsAfterEarlierId()
    {
        var timeProvider = KioskClock.CreateTimeProvider();
        string earlier = KioskIdGenerator.NewSessionId(timeProvider);
        timeProvider.Advance(TimeSpan.FromSeconds(1));

        string later = KioskIdGenerator.NewSessionId(timeProvider);

        string.CompareOrdinal(later, earlier).Should().BePositive();
    }

    [Test]
    public void NewSessionId_NullTimeProvider_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => KioskIdGenerator.NewSessionId(null!))
            .Should().Throw<ArgumentNullException>();
    }
}
