namespace GoldKiosk.Contracts.V1.Cloud.Kiosk;

/// <summary>
/// Response body for <c>GET /api/v1/kiosk/config</c> — the cloud-side config pull
/// (replaces legacy <c>KARAT-RANGE-PERCENTAGE</c>/<c>APP-STATUS</c>/<c>DISPENSER-BILLS</c>).
/// </summary>
/// <param name="KioskId">The kiosk's fleet identifier.</param>
/// <param name="Code">The kiosk's fleet code.</param>
/// <param name="Status">The fleet status, e.g. <c>live</c>, <c>onboarding</c>.</param>
/// <param name="IsMaintenance">Whether the kiosk is flagged for maintenance (blocks trading).</param>
/// <param name="KaratRange">The karat acceptance window.</param>
/// <param name="Features">The feature switches.</param>
/// <param name="Currency">The ISO 4217 currency this kiosk trades in.</param>
public sealed record KioskConfigResponse(
    Guid KioskId,
    string Code,
    string Status,
    bool IsMaintenance,
    KaratRangeDto KaratRange,
    KioskFeaturesDto Features,
    string Currency);
