using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Protocol;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Real.Protocol;

[TestFixture]
public sealed class BillMixPlannerTests
{
    [Test]
    public void Plan_AmountComposableFromInventory_ProducesTheExactGreedyMix()
    {
        var inventory = new Dictionary<int, int> { [100] = 2, [50] = 1, [20] = 1, [10] = 1, [5] = 1, [1] = 5 };

        BillMixPlan plan = BillMixPlanner.Plan(18_600, 100, inventory);

        plan.Feasible.Should().BeTrue();
        plan.Bills.Should().BeEquivalentTo(new Dictionary<int, int>
        {
            [100] = 1,
            [50] = 1,
            [20] = 1,
            [10] = 1,
            [5] = 1,
            [1] = 1,
        });
    }

    [Test]
    public void Plan_ZeroAmount_IsInfeasible()
    {
        BillMixPlan plan = BillMixPlanner.Plan(0, 100, new Dictionary<int, int> { [1] = 100 });

        plan.Feasible.Should().BeFalse();
        plan.Bills.Should().BeEmpty();
    }

    [Test]
    public void Plan_AmountWithSubMajorUnitRemainder_IsInfeasible()
    {
        BillMixPlan plan = BillMixPlanner.Plan(18_650, 100, new Dictionary<int, int> { [1] = 1000 });

        plan.Feasible.Should().BeFalse();
        plan.Bills.Should().BeEmpty();
    }

    [Test]
    public void Plan_RemainderNotCoverableBySmallNotes_IsInfeasibleWithEmptyBills()
    {
        var inventory = new Dictionary<int, int> { [5] = 1 };

        BillMixPlan plan = BillMixPlanner.Plan(700, 100, inventory);

        plan.Feasible.Should().BeFalse();
        plan.Bills.Should().BeEmpty();
    }

    [Test]
    public void Plan_AvailableCountAtTheReserve_ExcludesTheDenomination()
    {
        var inventory = new Dictionary<int, int> { [100] = 3, [50] = 10 };

        BillMixPlan plan = BillMixPlanner.Plan(30_000, 100, inventory, minimumBillsReserve: 3);

        plan.Feasible.Should().BeFalse("the 100s are gated out and 50×10 cannot reach 300");
    }

    [Test]
    public void Plan_CountAboveTheReserve_MayDrawTheCassetteAllTheWayDown()
    {
        var inventory = new Dictionary<int, int> { [100] = 3 };

        BillMixPlan plan = BillMixPlanner.Plan(30_000, 100, inventory, minimumBillsReserve: 2);

        // Legacy quirk kept verbatim: the reserve is checked once up front, after which the
        // plan may take every note in the cassette.
        plan.Feasible.Should().BeTrue();
        plan.Bills.Should().BeEquivalentTo(new Dictionary<int, int> { [100] = 3 });
    }

    [Test]
    public void Plan_GreedyChoice_NeverBacktracksToASmallerDenominationSolution()
    {
        var inventory = new Dictionary<int, int> { [50] = 1, [20] = 3 };

        BillMixPlan plan = BillMixPlanner.Plan(6_000, 100, inventory);

        // 20×3 would compose 60 exactly, but greedy grabs the 50 first and strands a 10.
        plan.Feasible.Should().BeFalse();
        plan.Bills.Should().BeEmpty();
    }

    [Test]
    public void Plan_NonPositiveDenominations_AreIgnored()
    {
        var inventory = new Dictionary<int, int> { [0] = 10, [-5] = 10, [10] = 10 };

        BillMixPlan plan = BillMixPlanner.Plan(3_000, 100, inventory);

        plan.Feasible.Should().BeTrue();
        plan.Bills.Should().BeEquivalentTo(new Dictionary<int, int> { [10] = 3 });
    }

    [Test]
    public void Plan_NegativeAmount_Throws()
    {
        FluentActions.Invoking(() => BillMixPlanner.Plan(-1, 100, new Dictionary<int, int>()))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Plan_MinorUnitsPerMajorUnitBelowOne_Throws()
    {
        FluentActions.Invoking(() => BillMixPlanner.Plan(100, 0, new Dictionary<int, int>()))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Plan_NegativeReserve_Throws()
    {
        FluentActions.Invoking(() => BillMixPlanner.Plan(100, 100, new Dictionary<int, int>(), minimumBillsReserve: -1))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Plan_NullInventory_Throws()
    {
        // Null-forgiving: deliberately passing null to exercise the guard clause.
        FluentActions.Invoking(() => BillMixPlanner.Plan(100, 100, null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Plan_ZeroDigitCurrency_ComposesWholeMajorUnits()
    {
        var inventory = new Dictionary<int, int> { [100] = 10, [5] = 20 };

        BillMixPlan plan = BillMixPlanner.Plan(865, 1, inventory);

        plan.Feasible.Should().BeTrue();
        plan.Bills.Should().BeEquivalentTo(new Dictionary<int, int> { [100] = 8, [5] = 13 });
    }
}
