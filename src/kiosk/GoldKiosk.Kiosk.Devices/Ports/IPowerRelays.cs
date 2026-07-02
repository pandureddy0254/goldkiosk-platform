using System.Diagnostics.CodeAnalysis;
using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// Power relay port. Real target: Advantech USB-4761 DAQ output bits, including the relay
/// that power-cycles the XRF analyser gun (the legacy recovery path for a wedged Vanta).
/// </summary>
public interface IPowerRelays : IKioskDevice
{
    /// <summary>Switches the shared device power rail on or off.</summary>
    /// <param name="on"><see langword="true"/> to energize the rail.</param>
    /// <param name="cancellationToken">Cancels waiting for relay confirmation.</param>
    /// <returns>A task that completes when the relay state is applied.</returns>
    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
        Justification = "'on' is the natural relay-state parameter name fixed by the GK-2 design note; VB interop is not a consumer of this edge library.")]
    Task SetDevicePowerAsync(bool on, CancellationToken cancellationToken = default);

    /// <summary>Power-cycles the XRF analyser (off, dwell, on) as a fault-recovery action.</summary>
    /// <param name="cancellationToken">Cancels waiting for the cycle to finish.</param>
    /// <returns>A task that completes when the analyser power has been cycled.</returns>
    Task ToggleAnalyserPowerAsync(CancellationToken cancellationToken = default);
}
