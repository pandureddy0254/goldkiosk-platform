namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>Outcome of a cash dispense operation.</summary>
/// <param name="Succeeded"><see langword="true"/> when every planned note was presented.</param>
/// <param name="Dispensed">Denomination (major currency units) → note count actually presented (may be partial on failure).</param>
/// <param name="FailureReason">Machine-readable failure reason when the dispense failed or was partial.</param>
public sealed record DispenseResult(
    bool Succeeded,
    IReadOnlyDictionary<int, int> Dispensed,
    string? FailureReason);
