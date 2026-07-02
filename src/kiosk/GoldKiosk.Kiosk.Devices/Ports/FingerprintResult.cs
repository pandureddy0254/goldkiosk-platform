namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>Outcome of a fingerprint capture attempt.</summary>
/// <param name="Succeeded"><see langword="true"/> when a usable sample was captured.</param>
/// <param name="SamplesNeeded">Total samples the device requires for a complete template (1 when a single capture suffices).</param>
/// <param name="Template">The enrolment template once capture is complete; <see langword="null"/> until then. Biometric PII — never log.</param>
public sealed record FingerprintResult(bool Succeeded, int SamplesNeeded, byte[]? Template);
