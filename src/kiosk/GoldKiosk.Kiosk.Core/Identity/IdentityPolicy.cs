using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Domain.Primitives;

namespace GoldKiosk.Kiosk.Core.Identity;

/// <summary>
/// Fail-closed KYC eligibility rules for the scanned identity document: adult, not
/// expired, government-issued. Failures carry the stable <c>kyc.*</c> reason codes.
/// There is no auto-approve path (design addendum: legacy's fabricated identity is banned).
/// </summary>
public static class IdentityPolicy
{
    /// <summary>The legal minimum customer age in years.</summary>
    public const int MinimumAgeYears = 18;

    /// <summary>Evaluates the document against the eligibility gates.</summary>
    /// <param name="facts">The scanned document facts.</param>
    /// <param name="today">Today's date at the kiosk.</param>
    /// <returns>Success, or the first failing <c>kyc.*</c> reason.</returns>
    public static Result Evaluate(IdentityDocumentFacts facts, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(facts);

        if (!facts.IsGovernmentId)
        {
            return Result.Failure(new DomainError(
                RejectionReasonCodes.KycNotGovtId, "The presented document is not a government-issued ID."));
        }

        if (facts.ExpiresOn < today)
        {
            return Result.Failure(new DomainError(
                RejectionReasonCodes.KycIdExpired, "The presented identity document is expired."));
        }

        int age = today.Year - facts.DateOfBirth.Year;
        if (facts.DateOfBirth > today.AddYears(-age))
        {
            age--;
        }

        return age < MinimumAgeYears
            ? Result.Failure(new DomainError(
                RejectionReasonCodes.KycUnderage, "The customer is under the legal minimum age."))
            : Result.Success();
    }
}
