using FluentAssertions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.TestKit;
using Microsoft.Extensions.Time.Testing;
using NUnit.Framework;

namespace GoldKiosk.Kiosk.Devices.Tests.Contracts;

/// <summary>
/// The <see cref="ICashDispenser"/> port contract: planning composes exact greedy bill
/// mixes, sub-major-unit amounts are infeasible, dispensing a feasible plan presents the
/// planned notes and dispensing an infeasible plan fails without presenting anything.
/// </summary>
public abstract class CashDispenserContract
{
    private ICashDispenser _dispenser = null!;

    protected FakeTimeProvider TimeProvider { get; private set; } = null!;

    protected SimulationOptions Simulation { get; private set; } = null!;

    protected abstract ICashDispenser CreateDispenser();

    [SetUp]
    public async Task ConnectDeviceAsync()
    {
        TimeProvider = KioskClock.CreateTimeProvider();
        Simulation = new SimulationOptions { LatencyMultiplier = 0 };
        _dispenser = CreateDispenser();
        await _dispenser.ConnectAsync();
    }

    [TearDown]
    public Task DisconnectDeviceAsync() => _dispenser.DisconnectAsync();

    [Test]
    public async Task PlanAsync_OneOfEachDenomination_ComposesTheExactGreedyMix()
    {
        BillMixPlan plan = await _dispenser.PlanAsync(18_600);

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
    public async Task PlanAsync_PlannedBills_AlwaysSumToTheRequestedAmount()
    {
        BillMixPlan plan = await _dispenser.PlanAsync(86_500);

        plan.Feasible.Should().BeTrue();
        plan.Bills.Sum(pair => (long)pair.Key * pair.Value * 100).Should().Be(86_500);
    }

    [Test]
    public async Task PlanAsync_SubDollarRemainder_IsInfeasible()
    {
        BillMixPlan plan = await _dispenser.PlanAsync(18_650);

        plan.Feasible.Should().BeFalse();
    }

    [Test]
    public async Task PlanAsync_ZeroAmount_IsInfeasible()
    {
        BillMixPlan plan = await _dispenser.PlanAsync(0);

        plan.Feasible.Should().BeFalse();
        plan.Bills.Should().BeEmpty();
    }

    [Test]
    public async Task PlanAsync_NegativeAmount_Throws()
    {
        await _dispenser.Awaiting(d => d.PlanAsync(-100))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task DispenseAsync_FeasiblePlan_PresentsExactlyThePlannedNotes()
    {
        BillMixPlan plan = await _dispenser.PlanAsync(18_600);

        DispenseResult result = await _dispenser.DispenseAsync(plan);

        result.Succeeded.Should().BeTrue();
        result.Dispensed.Should().BeEquivalentTo(plan.Bills);
        result.FailureReason.Should().BeNull();
    }

    [Test]
    public async Task DispenseAsync_InfeasiblePlan_FailsWithoutPresentingNotes()
    {
        BillMixPlan infeasible = await _dispenser.PlanAsync(18_650);

        DispenseResult result = await _dispenser.DispenseAsync(infeasible);

        result.Succeeded.Should().BeFalse();
        result.Dispensed.Should().BeEmpty();
        result.FailureReason.Should().NotBeNullOrEmpty();
    }
}
