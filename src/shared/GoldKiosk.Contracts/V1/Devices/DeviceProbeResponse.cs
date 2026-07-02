namespace GoldKiosk.Contracts.V1.Devices;

/// <summary>
/// Response body for <c>POST /api/v1/devices/{key}/probe</c> — a single-device diagnostic probe.
/// </summary>
/// <param name="Key">The probed device key, e.g. <c>scale</c>.</param>
/// <param name="Result">The probe outcome, e.g. <c>pass</c>, <c>fail</c>.</param>
/// <param name="ElapsedMs">How long the probe took, in milliseconds.</param>
public sealed record DeviceProbeResponse(string Key, string Result, long ElapsedMs);
