namespace GoldKiosk.Kiosk.Core.Cloud;

/// <summary>
/// The identity and trading facts a kiosk pulls from the cloud before it may transact:
/// who it is (tenant + kiosk), whether it is cleared to trade, and the trading window it
/// operates in. Cached last-good to <c>%ProgramData%</c> so the machine boots provisioned
/// when the cloud is unreachable (architecture-principles §4). Framework-free carrier for
/// <see cref="ICloudGateway"/>.
/// </summary>
/// <param name="TenantId">The owning tenant (read from the kiosk JWT's <c>tenant_id</c> claim).</param>
/// <param name="KioskId">The kiosk's fleet identifier.</param>
/// <param name="IsLive">
/// Whether the cloud cleared this kiosk to start customer transactions
/// (the status endpoint's <c>can_trade</c>); <see langword="false"/> surfaces an
/// out-of-service state to the UI.
/// </param>
/// <param name="OfferPercent">
/// The store margin percent applied to offers, when the cloud exposes it. Pricing internals
/// stay server-side today, so this is <c>0</c> unless a future contract surfaces it.
/// </param>
/// <param name="Currency">The ISO 4217 currency this kiosk trades in.</param>
/// <param name="KaratRange">The karat acceptance window.</param>
public sealed record KioskProvisioning(
    Guid TenantId,
    Guid KioskId,
    bool IsLive,
    decimal OfferPercent,
    string Currency,
    KaratRange KaratRange);
