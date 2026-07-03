using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Tuning for the real cash dispenser (Fujitsu F53 behind the ARCA Envoy RMI service) and the
/// greedy bill-mix planner ported from the legacy <c>GCCashDispenser.GetBillMix</c>.
/// </summary>
public sealed class CashDispenserOptions
{
    /// <summary>
    /// Denominations the planner may use, in major currency units. Order is irrelevant —
    /// planning is always greedy highest-first (legacy behavior).
    /// </summary>
    public IList<int> Denominations { get; init; } = [100, 50, 20, 10, 5, 2, 1];

    /// <summary>
    /// A cassette participates in a plan only while its available count is strictly greater
    /// than this reserve (legacy <c>MinimumNumberOfBillsRequired</c> gate). Note the legacy
    /// quirk, preserved: the gate is checked once up front, after which a plan may draw the
    /// cassette below the reserve.
    /// </summary>
    [Range(0, 10_000)]
    public int MinimumBillsReserve { get; set; }

    /// <summary>Minor currency units per major unit (100 for USD cents / INR paise).</summary>
    [Range(1, 10_000)]
    public int MinorUnitsPerMajorUnit { get; set; } = 100;

    /// <summary>
    /// Bill count assumed available per present cassette when planning. The F53 does not
    /// report note counts; the fleet's authoritative counts live in the cloud ledger and are
    /// reconciled upstream — this bound only stops the edge planner over-committing a single
    /// payout.
    /// </summary>
    [Range(1, 100_000)]
    public int AssumedCassetteBillCount { get; set; } = 2000;

    /// <summary>Timeout for one Envoy RMI call (device status, dispense).</summary>
    [Range(1000, 300_000)]
    public int RmiCallTimeoutMs { get; set; } = 60_000;
}
