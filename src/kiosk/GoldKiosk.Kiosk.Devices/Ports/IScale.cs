using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// Precision scale port. Real target: MT-SICS-style ASCII protocol over RS-232 COM5
/// (9600 8-N-1, <c>Q</c>/<c>Z</c> commands) with a proper settle discipline — the legacy
/// fixed-substring parse and culture bug are not carried forward.
/// </summary>
public interface IScale : IKioskDevice
{
    /// <summary>Reads the current weight.</summary>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The weight reading, including whether the value had settled.</returns>
    Task<WeightReading> GetWeightAsync(CancellationToken cancellationToken = default);

    /// <summary>Re-zeroes (tares) the scale.</summary>
    /// <param name="cancellationToken">Cancels the zero command.</param>
    /// <returns>A task that completes when the scale reports zeroed.</returns>
    Task ZeroAsync(CancellationToken cancellationToken = default);
}
