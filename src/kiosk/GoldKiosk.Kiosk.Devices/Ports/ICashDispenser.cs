using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// Cash dispenser port. Real target: Fujitsu F53 driven through the ARCA Envoy stack with
/// per-cassette verification. Planning and dispensing are separate so the payout screen can
/// disable the cash option when no feasible bill mix exists. Dispense commands are
/// idempotency-protected upstream — replay never double-dispenses.
/// </summary>
public interface ICashDispenser : IKioskDevice
{
    /// <summary>Computes a bill mix for the requested amount against current cassette inventory.</summary>
    /// <param name="amountMinor">Amount in minor currency units (e.g. cents).</param>
    /// <param name="cancellationToken">Cancels the planning call.</param>
    /// <returns>The plan; <c>Feasible</c> is <see langword="false"/> when the amount cannot be composed from available bills.</returns>
    Task<BillMixPlan> PlanAsync(long amountMinor, CancellationToken cancellationToken = default);

    /// <summary>Dispenses a previously computed bill mix.</summary>
    /// <param name="plan">The plan returned by <see cref="PlanAsync"/>.</param>
    /// <param name="cancellationToken">Cancels waiting for completion (hardware finishes the in-flight note safely).</param>
    /// <returns>The dispense outcome, including the bills actually presented.</returns>
    Task<DispenseResult> DispenseAsync(BillMixPlan plan, CancellationToken cancellationToken = default);
}
