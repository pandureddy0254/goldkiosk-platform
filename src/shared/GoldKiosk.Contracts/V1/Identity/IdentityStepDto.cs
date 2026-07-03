namespace GoldKiosk.Contracts.V1.Identity;

/// <summary>
/// One step in the identity sequence.
/// </summary>
/// <param name="Step">The step name: <c>id_scan</c>, <c>face_match</c>, <c>fingerprint</c> or <c>signature</c>.</param>
/// <param name="Status">The step status, e.g. <c>pending</c>, <c>in_progress</c>, <c>completed</c>, <c>failed</c>.</param>
/// <param name="Failure">The failure details when <paramref name="Status"/> is <c>failed</c>.</param>
public sealed record IdentityStepDto(string Step, string Status, IdentityFailureDto? Failure);
