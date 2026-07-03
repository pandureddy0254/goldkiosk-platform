using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real.Protocol;

/// <summary>
/// Greedy highest-denomination-first bill-mix planning, ported from the legacy
/// <c>GCCashDispenser.GetBillMix</c>/<c>SetBills</c>. Pure and deterministic — this is the
/// testable core behind <c>ICashDispenser.PlanAsync</c>; the ARCA Envoy SDK is only needed to
/// learn cassette availability and to physically dispense.
/// </summary>
public static class BillMixPlanner
{
    /// <summary>Computes a bill mix for the amount against the available inventory.</summary>
    /// <param name="amountMinor">Amount in minor currency units (e.g. cents). Must be non-negative.</param>
    /// <param name="minorUnitsPerMajorUnit">Minor units per major unit (100 for USD/INR).</param>
    /// <param name="availableBillsByDenomination">Available note count per denomination (major units).</param>
    /// <param name="minimumBillsReserve">
    /// Legacy <c>MinimumNumberOfBillsRequired</c> gate: a denomination participates only while
    /// its available count is <em>strictly greater</em> than this reserve. Legacy quirk kept
    /// verbatim: the gate is evaluated once up front, after which the plan may draw the
    /// cassette all the way down.
    /// </param>
    /// <returns>
    /// The plan. Infeasible when the amount is zero, not a whole number of major units, or
    /// cannot be composed greedily from the available notes (greedy — not optimal — matching
    /// legacy behavior: it never backtracks to smaller denominations).
    /// </returns>
    public static BillMixPlan Plan(
        long amountMinor,
        int minorUnitsPerMajorUnit,
        IReadOnlyDictionary<int, int> availableBillsByDenomination,
        int minimumBillsReserve = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amountMinor);
        ArgumentOutOfRangeException.ThrowIfLessThan(minorUnitsPerMajorUnit, 1);
        ArgumentNullException.ThrowIfNull(availableBillsByDenomination);
        ArgumentOutOfRangeException.ThrowIfNegative(minimumBillsReserve);

        if (amountMinor == 0 || amountMinor % minorUnitsPerMajorUnit != 0)
        {
            return new BillMixPlan(Feasible: false, Bills: new Dictionary<int, int>());
        }

        long remaining = amountMinor / minorUnitsPerMajorUnit;
        Dictionary<int, int> bills = [];
        foreach ((int denomination, int available) in availableBillsByDenomination
                     .Where(pair => pair.Key > 0)
                     .OrderByDescending(pair => pair.Key))
        {
            if (available <= minimumBillsReserve)
            {
                continue;
            }

            long wanted = remaining / denomination;
            int take = (int)Math.Min(wanted, available);
            if (take <= 0)
            {
                continue;
            }

            bills[denomination] = take;
            remaining -= (long)denomination * take;
        }

        return remaining == 0 && bills.Count > 0
            ? new BillMixPlan(Feasible: true, Bills: bills)
            : new BillMixPlan(Feasible: false, Bills: new Dictionary<int, int>());
    }
}
