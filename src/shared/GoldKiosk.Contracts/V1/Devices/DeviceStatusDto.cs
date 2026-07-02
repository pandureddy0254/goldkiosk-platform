namespace GoldKiosk.Contracts.V1.Devices;

/// <summary>
/// The status of one hardware device port.
/// </summary>
/// <param name="Key">The device key, e.g. <c>scale</c>, <c>metal_analyser</c>, <c>cash_dispenser</c>.</param>
/// <param name="Mode">The configured mode: <c>real</c> or <c>mock</c> (per-device bypass).</param>
/// <param name="State">The device state, e.g. <c>ready</c>, <c>busy</c>, <c>faulted</c>.</param>
/// <param name="Detail">Optional detail, e.g. adapter name (<c>vanta</c>) or fault code (<c>device_not_found</c>).</param>
public sealed record DeviceStatusDto(string Key, string Mode, string State, string? Detail);
