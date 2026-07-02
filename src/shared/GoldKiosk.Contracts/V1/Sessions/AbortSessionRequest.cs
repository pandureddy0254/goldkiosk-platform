namespace GoldKiosk.Contracts.V1.Sessions;

/// <summary>
/// Request body for <c>POST /api/v1/sessions/{id}/abort</c>.
/// </summary>
/// <param name="Reason">The abort reason: <c>timeout</c>, <c>user_cancel</c>, <c>operator</c> or <c>fault</c>.</param>
/// <param name="ReturnItem">Whether a held item must be returned to the customer.</param>
public sealed record AbortSessionRequest(string Reason, bool ReturnItem);
