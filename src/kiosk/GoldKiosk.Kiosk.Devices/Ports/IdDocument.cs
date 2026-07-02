namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// Fields extracted from a scanned identity document. Restricted PII: encrypted at rest,
/// masked in UI, never logged, never sent to third-party APIs (security standard §PII).
/// </summary>
/// <param name="FirstName">Given name as printed on the document.</param>
/// <param name="LastName">Family name as printed on the document.</param>
/// <param name="DateOfBirth">Holder's date of birth (age gate is enforced upstream).</param>
/// <param name="ExpiresOn">Document expiry date.</param>
/// <param name="DocumentNumber">Document number (e.g. driving licence number).</param>
/// <param name="IsGovernmentId"><see langword="true"/> when the document class is a government-issued ID.</param>
/// <param name="PortraitImage">Encoded portrait crop from the document, used for face match.</param>
public sealed record IdDocument(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    DateOnly ExpiresOn,
    string DocumentNumber,
    bool IsGovernmentId,
    byte[] PortraitImage);
