namespace GoldKiosk.Contracts.V1.Cloud.Kiosk;

/// <summary>
/// Feature switches served with the kiosk config pull.
/// </summary>
/// <param name="PawnEnabled">Whether pawn transactions are offered.</param>
/// <param name="CryptoEnabled">Whether crypto-for-gold is enabled (off by default, per-tenant opt-in).</param>
/// <param name="PayoutMethods">The payout methods available, e.g. <c>cash</c>, <c>bank_transfer</c>.</param>
public sealed record KioskFeaturesDto(
    bool PawnEnabled,
    bool CryptoEnabled,
    IReadOnlyList<string> PayoutMethods);
