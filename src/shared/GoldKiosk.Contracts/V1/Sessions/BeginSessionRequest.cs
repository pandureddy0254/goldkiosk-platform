namespace GoldKiosk.Contracts.V1.Sessions;

/// <summary>
/// Request body for <c>POST /api/v1/sessions</c> — begins a session at the welcome tap.
/// </summary>
/// <param name="Locale">The BCP 47 locale selected or defaulted at the attract loop, e.g. <c>en-US</c>.</param>
/// <param name="AttractSource">How the session was initiated, e.g. <c>touch</c>.</param>
/// <param name="KioskTime">The kiosk's local wall-clock time at the tap (clock-drift signal).</param>
public sealed record BeginSessionRequest(string Locale, string AttractSource, DateTimeOffset KioskTime);
