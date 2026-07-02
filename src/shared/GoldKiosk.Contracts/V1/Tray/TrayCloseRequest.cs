namespace GoldKiosk.Contracts.V1.Tray;

/// <summary>
/// Request body for <c>POST /api/v1/sessions/{id}/tray/close</c> — clubbed payload #2:
/// the close command, the has-item flag, and batched client telemetry.
/// <c>HasItem = false</c> means close-without-item (customer backed out).
/// </summary>
/// <param name="Command">The command discriminator, <c>tray_close</c>.</param>
/// <param name="HasItem">Whether the customer placed an item; <see langword="true"/> triggers analysis.</param>
/// <param name="ClientEvents">Batched UI telemetry events accumulated since the tray opened.</param>
public sealed record TrayCloseRequest(
    string Command,
    bool HasItem,
    IReadOnlyList<ClientEventDto> ClientEvents);
