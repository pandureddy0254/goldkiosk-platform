using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// Government ID scanner port. Real targets behind this one port: Acuant ScanShell
/// (in-process SDK) and the 3M/Gemalto full-page reader (visible/IR/UV) — the composition
/// root picks the driver for the fitted scanner. Scan output is restricted PII: encrypted at
/// rest, never logged, never sent to third-party APIs.
/// </summary>
public interface IIdScanner : IKioskDevice
{
    /// <summary>Scans the inserted identity document and extracts its fields.</summary>
    /// <param name="cancellationToken">Cancels the scan.</param>
    /// <returns>The scan outcome; the document is present only when the scan succeeded.</returns>
    Task<IdScanResult> ScanAsync(CancellationToken cancellationToken = default);
}
