using System.Collections.ObjectModel;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>
/// Simulated cash dispenser: plans a greedy bill mix over the 100/50/20/10/5/1 denominations
/// with unlimited cassette inventory. Amounts with a sub-bill remainder (non-whole major
/// units) or of zero are infeasible; dispensing a feasible plan always succeeds.
/// </summary>
public sealed class SimulatedCashDispenser : SimulatedDeviceBase, ICashDispenser
{
    private const int MinorUnitsPerMajorUnit = 100;
    private static readonly int[] _denominations = [100, 50, 20, 10, 5, 1];

    /// <summary>Initializes the simulated dispenser.</summary>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays.</param>
    public SimulatedCashDispenser(SimulationOptions simulation, TimeProvider timeProvider)
        : base(DeviceKeys.CashDispenser, "Cash dispenser (simulated)", isCritical: false, simulation, timeProvider)
    {
    }

    /// <inheritdoc />
    public async Task<BillMixPlan> PlanAsync(long amountMinor, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amountMinor);
        ThrowIfFaulted();
        await DelayAsync(150, cancellationToken).ConfigureAwait(false);

        if (amountMinor == 0 || amountMinor % MinorUnitsPerMajorUnit != 0)
        {
            return new BillMixPlan(Feasible: false, Bills: ReadOnlyDictionary<int, int>.Empty);
        }

        long remaining = amountMinor / MinorUnitsPerMajorUnit;
        Dictionary<int, int> bills = [];
        foreach (int denomination in _denominations)
        {
            var count = (int)(remaining / denomination);
            if (count > 0)
            {
                bills[denomination] = count;
                remaining -= (long)count * denomination;
            }
        }

        return new BillMixPlan(Feasible: remaining == 0, Bills: bills);
    }

    /// <inheritdoc />
    public async Task<DispenseResult> DispenseAsync(BillMixPlan plan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ThrowIfFaulted();
        await DelayAsync(2500, cancellationToken).ConfigureAwait(false);

        return plan.Feasible
            ? new DispenseResult(Succeeded: true, Dispensed: plan.Bills, FailureReason: null)
            : new DispenseResult(
                Succeeded: false,
                Dispensed: ReadOnlyDictionary<int, int>.Empty,
                FailureReason: "plan_not_feasible");
    }
}
