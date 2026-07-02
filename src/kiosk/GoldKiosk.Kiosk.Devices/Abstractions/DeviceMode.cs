namespace GoldKiosk.Kiosk.Devices.Abstractions;

/// <summary>
/// Whether a device slot is served by the real hardware driver or the simulator.
/// Resolved per device from layered configuration (ADR 0004); composition happens once at
/// startup in the Kiosk.Api host, and any session run with at least one
/// <see cref="Mock"/> device is flagged as a test transaction.
/// </summary>
public enum DeviceMode
{
    /// <summary>The real hardware driver talks to the physical device.</summary>
    Real,

    /// <summary>A simulator honours the port contract without physical hardware.</summary>
    Mock,
}
