using FluentAssertions;
using GoldKiosk.Domain.Assay;
using NUnit.Framework;

namespace GoldKiosk.Domain.Tests.Assay;

[TestFixture]
public sealed class RejectionReasonCodeMapTests
{
    [TestCase(RejectionReason.GoldPlated, RejectionReasonCodeMap.GoldPlated)]
    [TestCase(RejectionReason.LessThanAcceptableGoldKarat, RejectionReasonCodeMap.InsufficientPurity)]
    [TestCase(RejectionReason.LessThanAcceptableSilverPercent, RejectionReasonCodeMap.InsufficientPurity)]
    [TestCase(RejectionReason.ContainsTungsten, RejectionReasonCodeMap.Unidentified)]
    [TestCase(RejectionReason.ContainsPlatinum, RejectionReasonCodeMap.Unidentified)]
    [TestCase(RejectionReason.ContainsIridium, RejectionReasonCodeMap.Unidentified)]
    [TestCase(RejectionReason.ContainsRhodium, RejectionReasonCodeMap.Unidentified)]
    [TestCase(RejectionReason.ContainsRuthenium, RejectionReasonCodeMap.Unidentified)]
    [TestCase(RejectionReason.ContainsPalladium, RejectionReasonCodeMap.Unidentified)]
    [TestCase(RejectionReason.ContainsLead, RejectionReasonCodeMap.Unidentified)]
    [TestCase(RejectionReason.ContainsMolybdenum, RejectionReasonCodeMap.Unidentified)]
    [TestCase(RejectionReason.ContainsBismuth, RejectionReasonCodeMap.Unidentified)]
    [TestCase(RejectionReason.ContainsCadmium, RejectionReasonCodeMap.Unidentified)]
    [TestCase(RejectionReason.ContainsIron, RejectionReasonCodeMap.Unidentified)]
    [TestCase(RejectionReason.ContainsManganese, RejectionReasonCodeMap.Unidentified)]
    [TestCase(RejectionReason.ContainsIndium, RejectionReasonCodeMap.Unidentified)]
    [TestCase(RejectionReason.UnacceptableVolumeError, RejectionReasonCodeMap.Unidentified)]
    public void ToCode_MapsReasonToStableCode(RejectionReason reason, string expectedCode)
    {
        RejectionReasonCodeMap.ToCode(reason).Should().Be(expectedCode);
    }
}
