using FluentAssertions;
using GoldKiosk.Domain.Assay;
using NUnit.Framework;

namespace GoldKiosk.Domain.Tests.Assay;

[TestFixture]
public sealed class KaratBandMapperTests
{
    private static IEnumerable<TestCaseData> BandBoundaryCases()
    {
        yield return new TestCaseData(-0.1m, KaratBandMapper.None);
        yield return new TestCaseData(0m, KaratBandMapper.Gold0);
        yield return new TestCaseData(7.99m, KaratBandMapper.Gold0);
        yield return new TestCaseData(8m, KaratBandMapper.Gold8To9);
        yield return new TestCaseData(9.99m, KaratBandMapper.Gold8To9);
        yield return new TestCaseData(10m, KaratBandMapper.Gold10);
        yield return new TestCaseData(13.99m, KaratBandMapper.Gold10);
        yield return new TestCaseData(14m, KaratBandMapper.Gold14);
        yield return new TestCaseData(17.99m, KaratBandMapper.Gold14);
        yield return new TestCaseData(18m, KaratBandMapper.Gold18);
        yield return new TestCaseData(21.99m, KaratBandMapper.Gold18);
        yield return new TestCaseData(22m, KaratBandMapper.Gold22);
        yield return new TestCaseData(24m, KaratBandMapper.Gold22);
        yield return new TestCaseData(24.01m, KaratBandMapper.None);
    }

    [TestCaseSource(nameof(BandBoundaryCases))]
    public void Map_KaratBoundary_ReturnsExpectedBand(decimal karat, string expected)
    {
        KaratBandMapper.Map(karat).Should().Be(expected);
    }

    [Test]
    public void Map_WithZeroRange_MapsDirectly()
    {
        KaratBandMapper.Map(12m, 0m).Should().Be(KaratBandMapper.Gold10);
    }

    [Test]
    public void Map_WithRangeTolerance_SnapsToNominalStamp()
    {
        // 7.5 falls in the 8-karat tolerance band [7.2, 8.8] at 10% -> gold-8-9ktg.
        KaratBandMapper.Map(7.5m, 10m).Should().Be(KaratBandMapper.Gold8To9);
    }

    [Test]
    public void Map_WithRangeTolerance_NoNominalMatch_MapsOriginalKarat()
    {
        // 12 is outside every nominal tolerance band at 10% -> maps directly to gold-10ktg.
        KaratBandMapper.Map(12m, 10m).Should().Be(KaratBandMapper.Gold10);
    }

    [Test]
    public void Map_NegativeRange_Throws()
    {
        FluentActions.Invoking(() => KaratBandMapper.Map(18m, -1m))
            .Should().Throw<ArgumentOutOfRangeException>();
    }
}
