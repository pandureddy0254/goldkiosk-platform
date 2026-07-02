namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>Outcome of an identity document scan.</summary>
/// <param name="Succeeded"><see langword="true"/> when the document was read and parsed.</param>
/// <param name="FailureReason">Machine-readable failure reason when the scan failed (e.g. unreadable, unsupported document).</param>
/// <param name="Document">The extracted document; <see langword="null"/> when the scan failed. Restricted PII — never log.</param>
public sealed record IdScanResult(bool Succeeded, string? FailureReason, IdDocument? Document);
