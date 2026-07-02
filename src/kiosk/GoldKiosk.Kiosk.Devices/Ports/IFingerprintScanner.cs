using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// Fingerprint scanner port (config-gated identity step). Real target: FlexCode SDK
/// (serial + activation licensing) with the legacy three-strike retry rule enforced upstream.
/// Templates are restricted biometric PII — never logged.
/// </summary>
public interface IFingerprintScanner : IKioskDevice
{
    /// <summary>Captures one fingerprint sample and, when enough samples exist, the enrolment template.</summary>
    /// <param name="cancellationToken">Cancels the capture.</param>
    /// <returns>The capture outcome, including how many further samples are needed.</returns>
    Task<FingerprintResult> CaptureAsync(CancellationToken cancellationToken = default);
}
