namespace GoldKiosk.Kiosk.Core.Identity;

/// <summary>
/// The eligibility-relevant facts from a scanned identity document, expressed with BCL
/// types only so Kiosk.Core stays free of device types. Never logged.
/// </summary>
/// <param name="DateOfBirth">The document holder's date of birth.</param>
/// <param name="ExpiresOn">The document expiry date.</param>
/// <param name="IsGovernmentId"><see langword="true"/> when the document is a government-issued ID.</param>
public sealed record IdentityDocumentFacts(DateOnly DateOfBirth, DateOnly ExpiresOn, bool IsGovernmentId);
