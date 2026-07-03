namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>Progress notification emitted while an XRF analysis run is executing.</summary>
/// <param name="Percent">Overall completion, 0–100.</param>
/// <param name="Stage">Stage identifier surfaced to the UI checklist: <c>item_detected</c>, <c>authenticating</c>, <c>pricing</c>.</param>
public sealed record AnalysisProgress(int Percent, string Stage);
