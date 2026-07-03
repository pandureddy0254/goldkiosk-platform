using FluentAssertions;
using GoldKiosk.Domain.Assay;
using NUnit.Framework;

namespace GoldKiosk.Domain.Tests.Assay;

[TestFixture]
public sealed class AssayRejectionPolicyTests
{
    private AssayRejectionPolicy _policy = null!;

    [SetUp]
    public void SetUp() => _policy = new AssayRejectionPolicy(AssayOptions.Default);

    private static IEnumerable<TestCaseData> ElementThresholdCases()
    {
        yield return new TestCaseData("W", 2m, RejectionReason.ContainsTungsten);
        yield return new TestCaseData("Pt", 2m, RejectionReason.ContainsPlatinum);
        yield return new TestCaseData("Ir", 2m, RejectionReason.ContainsIridium);
        yield return new TestCaseData("Rh", 4m, RejectionReason.ContainsRhodium);
        yield return new TestCaseData("Ru", 2m, RejectionReason.ContainsRuthenium);
        yield return new TestCaseData("Pd", 2m, RejectionReason.ContainsPalladium);
        yield return new TestCaseData("Pb", 2m, RejectionReason.ContainsLead);
        yield return new TestCaseData("Mo", 2m, RejectionReason.ContainsMolybdenum);
        yield return new TestCaseData("Bi", 2m, RejectionReason.ContainsBismuth);
        yield return new TestCaseData("Cd", 2m, RejectionReason.ContainsCadmium);
        yield return new TestCaseData("Fe", 10m, RejectionReason.ContainsIron);
        yield return new TestCaseData("Mn", 2m, RejectionReason.ContainsManganese);
        yield return new TestCaseData("In", 2m, RejectionReason.ContainsIndium);
    }

    [TestCaseSource(nameof(ElementThresholdCases))]
    public void Evaluate_ElementJustOverThreshold_Rejects(string symbol, decimal threshold, RejectionReason expected)
    {
        var readings = new ElementReading[] { new(symbol, threshold + 0.01m, 0m) };

        var reason = _policy.Evaluate(readings, offerKarat: 20m);

        reason.Should().Be(expected);
    }

    [TestCaseSource(nameof(ElementThresholdCases))]
    public void Evaluate_ElementAtThreshold_Accepts(string symbol, decimal threshold, RejectionReason expected)
    {
        _ = expected;
        var readings = new ElementReading[] { new(symbol, threshold, 0m) };

        var reason = _policy.Evaluate(readings, offerKarat: 20m);

        reason.Should().BeNull();
    }

    [Test]
    public void Evaluate_OfferKaratBelowFloor_RejectsInsufficientPurity()
    {
        var readings = new ElementReading[] { new("Au", 40m, 0m) };

        var reason = _policy.Evaluate(readings, offerKarat: 9.19m);

        reason.Should().Be(RejectionReason.LessThanAcceptableGoldKarat);
        RejectionReasonCodeMap.ToCode(reason!.Value).Should().Be(RejectionReasonCodeMap.InsufficientPurity);
    }

    [Test]
    public void Evaluate_OfferKaratAtFloor_Accepts()
    {
        var readings = new ElementReading[] { new("Au", 40m, 0m) };

        var reason = _policy.Evaluate(readings, offerKarat: 9.2m);

        reason.Should().BeNull();
    }

    [Test]
    public void Evaluate_SilverBelowFloor_RejectsInsufficientPurity()
    {
        var readings = new ElementReading[] { new("Ag", 79.99m, 0m) };

        var reason = _policy.Evaluate(readings, silverPercent: 79.99m);

        reason.Should().Be(RejectionReason.LessThanAcceptableSilverPercent);
    }

    [Test]
    public void Evaluate_SilverAtFloor_Accepts()
    {
        var readings = new ElementReading[] { new("Ag", 80m, 0m) };

        var reason = _policy.Evaluate(readings, silverPercent: 80m);

        reason.Should().BeNull();
    }

    [Test]
    public void Evaluate_GoldPlated_RejectsGoldPlated()
    {
        var readings = new ElementReading[] { new("Au", 90m, 0m) };

        var reason = _policy.Evaluate(readings, offerKarat: 20m, isGoldPlated: true);

        reason.Should().Be(RejectionReason.GoldPlated);
    }

    [Test]
    public void Evaluate_CleanGold_Accepts()
    {
        var readings = new ElementReading[] { new("Au", 92m, 0m), new("Cu", 8m, 0m) };

        var reason = _policy.Evaluate(readings, offerKarat: 22m);

        reason.Should().BeNull();
    }

    [Test]
    public void EvaluateAll_MultipleReasons_ReturnsInLegacyOrder()
    {
        var readings = new ElementReading[] { new("W", 5m, 0m), new("Au", 10m, 0m) };

        var reasons = _policy.EvaluateAll(readings, offerKarat: 5m, silverPercent: 70m, isGoldPlated: true);

        reasons.Should().Equal(
            RejectionReason.ContainsTungsten,
            RejectionReason.LessThanAcceptableGoldKarat,
            RejectionReason.LessThanAcceptableSilverPercent,
            RejectionReason.GoldPlated);
    }

    [Test]
    public void Evaluate_MultipleReasons_ReturnsFirst()
    {
        var readings = new ElementReading[] { new("W", 5m, 0m) };

        var reason = _policy.Evaluate(readings, offerKarat: 5m);

        reason.Should().Be(RejectionReason.ContainsTungsten);
    }
}
