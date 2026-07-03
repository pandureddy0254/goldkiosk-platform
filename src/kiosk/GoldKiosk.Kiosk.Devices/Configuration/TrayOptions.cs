using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Tuning for the real tray driver (stepper-board drawer with the tray-closed sensor bit).
/// Commands are the legacy stepper protocol (<c>ld</c> open / <c>lu</c> close, <c>*</c> ack).
/// </summary>
public sealed class TrayOptions
{
    /// <summary>Timeout waiting for the stepper board's <c>*</c> command acknowledgement.</summary>
    [Range(100, 60_000)]
    public int AckTimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Timeout for the tray to reach its commanded end position as confirmed by the sensor
    /// bit. On expiry the tray reports <c>Blocked</c> (legacy polled the sensor once after a
    /// fixed 4 s wait; the driver now polls until confirmed or timed out).
    /// </summary>
    [Range(500, 120_000)]
    public int MotionTimeoutMs { get; set; } = 20_000;

    /// <summary>Sensor poll interval while waiting for end of travel.</summary>
    [Range(50, 5000)]
    public int SensorPollIntervalMs { get; set; } = 250;

    /// <summary>
    /// Travel dwell used when no sensor board is wired into the driver (degraded verify:
    /// ack + fixed wait, mirroring the legacy 4 s sensor re-check delay).
    /// </summary>
    [Range(0, 60_000)]
    public int FallbackTravelDelayMs { get; set; } = 4000;
}
