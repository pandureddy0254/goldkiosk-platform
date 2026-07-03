namespace GoldKiosk.Contracts.V1.Cloud.Kiosk;

/// <summary>
/// Response body for <c>GET /api/v1/kiosk/status</c> — the lightweight trading gate
/// (replaces legacy <c>APP-STATUS</c>/<c>CHECK_STORE_STATUS_ONLINE</c>).
/// </summary>
/// <param name="Status">The fleet status, e.g. <c>live</c>.</param>
/// <param name="IsMaintenance">Whether the kiosk is flagged for maintenance.</param>
/// <param name="CanTrade">Whether the kiosk may start customer transactions.</param>
/// <param name="ServerTime">The cloud time, for kiosk clock-drift checks.</param>
public sealed record KioskStatusResponse(
    string Status,
    bool IsMaintenance,
    bool CanTrade,
    DateTimeOffset ServerTime);
