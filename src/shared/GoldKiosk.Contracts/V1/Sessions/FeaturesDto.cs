namespace GoldKiosk.Contracts.V1.Sessions;

/// <summary>
/// Feature flags in effect for a session (config-layer driven, cloud-overridable).
/// </summary>
/// <param name="PawnEnabled">Whether the pawn service is offered.</param>
/// <param name="CryptoEnabled">Whether crypto payout is offered (tenant opt-in).</param>
/// <param name="FingerprintRequired">Whether the fingerprint identity step is required.</param>
/// <param name="PayoutMethods">The payout methods available, e.g. <c>cash</c>, <c>bank_transfer</c>, <c>debit_card</c>.</param>
public sealed record FeaturesDto(
    bool PawnEnabled,
    bool CryptoEnabled,
    bool FingerprintRequired,
    IReadOnlyList<string> PayoutMethods);
