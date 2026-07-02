using GoldKiosk.Contracts.V1.Identity;

namespace GoldKiosk.Contracts.V1.Events;

/// <summary>
/// SignalR <c>identity_progress</c> payload — an identity step changed status.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="Sequence">The per-session monotonic event sequence.</param>
/// <param name="Step">The step name: <c>id_scan</c>, <c>face_match</c>, <c>fingerprint</c> or <c>signature</c>.</param>
/// <param name="Status">The step status, e.g. <c>in_progress</c>, <c>completed</c>, <c>failed</c>.</param>
/// <param name="Failure">The failure details when <paramref name="Status"/> is <c>failed</c>.</param>
public sealed record IdentityProgressEvent(
    string SessionId,
    long Sequence,
    string Step,
    string Status,
    IdentityFailureDto? Failure);
