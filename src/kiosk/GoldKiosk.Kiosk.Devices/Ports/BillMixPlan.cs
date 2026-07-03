namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>A planned combination of bills for a cash payout.</summary>
/// <param name="Feasible"><see langword="true"/> when the amount can be composed from available cassette inventory.</param>
/// <param name="Bills">Denomination (major currency units) → note count. Empty when infeasible.</param>
public sealed record BillMixPlan(bool Feasible, IReadOnlyDictionary<int, int> Bills);
