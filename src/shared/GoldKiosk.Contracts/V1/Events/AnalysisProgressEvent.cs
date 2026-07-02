namespace GoldKiosk.Contracts.V1.Events;

/// <summary>
/// SignalR <c>analysis_progress</c> payload — item analysis advanced a step.
/// No internals (weight, karat, percentages) are exposed.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="Sequence">The per-session monotonic event sequence.</param>
/// <param name="Stage">The analysis stage, e.g. <c>item_detected</c>, <c>authenticating</c>, <c>pricing</c>.</param>
/// <param name="Progress">The overall progress percentage, 0–100.</param>
/// <param name="Display">The customer-facing checklist text, e.g. <c>Verifying authenticity</c>.</param>
public sealed record AnalysisProgressEvent(
    string SessionId,
    long Sequence,
    string Stage,
    int Progress,
    string Display);
