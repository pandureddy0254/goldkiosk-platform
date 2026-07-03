using FluentAssertions;
using GoldKiosk.Domain.Primitives;
using GoldKiosk.Kiosk.Core.Identity;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Core.Tests.Identity;

[TestFixture]
public sealed class IdentityPolicyTests
{
    private static readonly DateOnly Today = new(2026, 7, 2);

    [Test]
    public void Evaluate_NonGovernmentDocument_RejectsAsNotGovtIdBeforeAllOtherGates()
    {
        var facts = new IdentityDocumentFacts(
            new DateOnly(1990, 1, 1), ExpiresOn: Today.AddYears(-1), IsGovernmentId: false);

        Result result = IdentityPolicy.Evaluate(facts, Today);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("kyc.not_govt_id");
    }

    [Test]
    public void Evaluate_ExpiredDocument_RejectsAsIdExpired()
    {
        var facts = new IdentityDocumentFacts(
            new DateOnly(1990, 1, 1), ExpiresOn: Today.AddDays(-1), IsGovernmentId: true);

        Result result = IdentityPolicy.Evaluate(facts, Today);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("kyc.id_expired");
    }

    [Test]
    public void Evaluate_DocumentExpiringToday_PassesTheExpiryGate()
    {
        var facts = new IdentityDocumentFacts(
            new DateOnly(1990, 1, 1), ExpiresOn: Today, IsGovernmentId: true);

        Result result = IdentityPolicy.Evaluate(facts, Today);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Evaluate_EighteenthBirthdayToday_Passes()
    {
        var facts = new IdentityDocumentFacts(
            new DateOnly(2008, 7, 2), ExpiresOn: Today.AddYears(4), IsGovernmentId: true);

        Result result = IdentityPolicy.Evaluate(facts, Today);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Evaluate_DayBeforeEighteenthBirthday_RejectsAsUnderage()
    {
        var facts = new IdentityDocumentFacts(
            new DateOnly(2008, 7, 3), ExpiresOn: Today.AddYears(4), IsGovernmentId: true);

        Result result = IdentityPolicy.Evaluate(facts, Today);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("kyc.underage");
    }

    [Test]
    public void Evaluate_LeapDayBirthBeforeAnniversary_RejectsAsUnderage()
    {
        var facts = new IdentityDocumentFacts(
            new DateOnly(2008, 2, 29), ExpiresOn: new DateOnly(2030, 1, 1), IsGovernmentId: true);

        Result result = IdentityPolicy.Evaluate(facts, new DateOnly(2026, 2, 28));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("kyc.underage");
    }

    [Test]
    public void Evaluate_LeapDayBirthAfterAnniversary_Passes()
    {
        var facts = new IdentityDocumentFacts(
            new DateOnly(2008, 2, 29), ExpiresOn: new DateOnly(2030, 1, 1), IsGovernmentId: true);

        Result result = IdentityPolicy.Evaluate(facts, new DateOnly(2026, 3, 1));

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Evaluate_ClearlyAdultWithValidGovtId_Passes()
    {
        var facts = new IdentityDocumentFacts(
            new DateOnly(1994, 3, 15), ExpiresOn: Today.AddYears(4), IsGovernmentId: true);

        Result result = IdentityPolicy.Evaluate(facts, Today);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Evaluate_NullFacts_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => IdentityPolicy.Evaluate(null!, Today))
            .Should().Throw<ArgumentNullException>();
    }
}
