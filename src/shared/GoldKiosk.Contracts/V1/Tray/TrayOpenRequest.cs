namespace GoldKiosk.Contracts.V1.Tray;

/// <summary>
/// Request body for <c>POST /api/v1/sessions/{id}/tray/open</c> — clubbed payload #1:
/// the tray-open command plus everything selected before the tray goes up.
/// No per-selection calls precede this.
/// </summary>
/// <param name="Command">The command discriminator, <c>tray_open</c>.</param>
/// <param name="Setup">Everything the customer selected before the tray opens.</param>
/// <param name="ClientEvents">Batched UI telemetry events accumulated since the session began.</param>
public sealed record TrayOpenRequest(
    string Command,
    SetupDto Setup,
    IReadOnlyList<ClientEventDto> ClientEvents);
