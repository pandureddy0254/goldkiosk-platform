using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real cash dispenser driver stub. Phase 4 target SDK: Fujitsu F53 driven through the
/// ARCA Envoy stack with greedy bill-mix planning and per-cassette verification.
/// </summary>
public sealed class RealCashDispenser : RealDeviceStub, ICashDispenser
{
    /// <summary>Initializes the stub.</summary>
    public RealCashDispenser()
        : base(DeviceKeys.CashDispenser, "Cash dispenser (ARCA Envoy F53)", isCritical: false)
    {
    }

    /// <inheritdoc />
    public Task<BillMixPlan> PlanAsync(long amountMinor, CancellationToken cancellationToken = default) =>
        throw NotWired();

    /// <inheritdoc />
    public Task<DispenseResult> DispenseAsync(BillMixPlan plan, CancellationToken cancellationToken = default) =>
        throw NotWired();
}
