using GoldKiosk.Contracts.V1.Common;

namespace GoldKiosk.Contracts.V1.Identity;

/// <summary>
/// Details of a failed identity step. Identity is fail-closed: retries then abort with
/// item return — never auto-approval.
/// </summary>
/// <param name="ReasonCode">The stable reason code (see <see cref="RejectionReasonCodes"/>), e.g. <c>kyc.face_mismatch</c>.</param>
/// <param name="RetriesLeft">How many retries remain for this step.</param>
/// <param name="Recovery">The recovery routing hint for the UI, e.g. <c>retry_face_match</c>.</param>
public sealed record IdentityFailureDto(string ReasonCode, int RetriesLeft, string Recovery);
