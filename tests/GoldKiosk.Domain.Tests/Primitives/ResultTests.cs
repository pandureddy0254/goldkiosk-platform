using FluentAssertions;
using GoldKiosk.Domain.Primitives;
using NUnit.Framework;

namespace GoldKiosk.Domain.Tests.Primitives;

[TestFixture]
public sealed class ResultTests
{
    private static readonly DomainError Error = new("offer.expired", "The offer has expired.");

    [Test]
    public void Success_ReportsSuccessAndNotFailure()
    {
        Result result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
    }

    [Test]
    public void Success_AccessingError_Throws()
    {
        Result result = Result.Success();

        result.Invoking(r => r.Error).Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Failure_CarriesTheDomainError()
    {
        Result result = Result.Failure(Error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error);
    }

    [Test]
    public void Failure_NullError_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => Result.Failure(null!)).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void SuccessOfT_CarriesTheValue()
    {
        Result<int> result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Test]
    public void SuccessOfT_AccessingError_Throws()
    {
        Result<int> result = Result.Success(42);

        result.Invoking(r => r.Error).Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void FailureOfT_AccessingValue_Throws()
    {
        Result<int> result = Result.Failure<int>(Error);

        result.Invoking(r => r.Value).Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void FailureOfT_CarriesTheDomainError()
    {
        Result<int> result = Result.Failure<int>(Error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(Error);
    }

    [Test]
    public void FailureOfT_NullError_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => Result.Failure<int>(null!)).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void DomainError_SameCodeAndMessage_AreEqual()
    {
        new DomainError("a.b", "m").Should().Be(new DomainError("a.b", "m"));
    }
}
