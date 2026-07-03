namespace GoldKiosk.Contracts.V1.Events;

/// <summary>
/// SignalR <c>settlement_progress</c> payload — bagging/dispensing/transferring advanced.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="Sequence">The per-session monotonic event sequence.</param>
/// <param name="Stage">The settlement stage, e.g. <c>bagging</c>, <c>dispensing</c>, <c>transferring</c>.</param>
/// <param name="Display">The customer-facing text, e.g. <c>Dispensing your cash</c>.</param>
/// <param name="Bills">The bill mix being dispensed (denomination → count), for cash payouts.</param>
public sealed record SettlementProgressEvent(
    string SessionId,
    long Sequence,
    string Stage,
    string Display,
    IReadOnlyDictionary<string, int>? Bills);
