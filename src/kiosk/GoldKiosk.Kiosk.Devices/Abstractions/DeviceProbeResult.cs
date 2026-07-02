namespace GoldKiosk.Kiosk.Devices.Abstractions;

/// <summary>
/// Outcome of a single-device diagnostic probe (used by <c>POST /devices/{key}/probe</c>
/// and the Diagnostics self-test).
/// </summary>
/// <param name="Passed"><see langword="true"/> when the device responded and passed its self-check.</param>
/// <param name="ElapsedMs">Wall-clock duration of the probe in milliseconds.</param>
/// <param name="Detail">Optional human-readable detail (failure reason, firmware note). Never PII.</param>
public sealed record DeviceProbeResult(bool Passed, long ElapsedMs, string? Detail = null);
