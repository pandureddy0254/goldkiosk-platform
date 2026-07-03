namespace GoldKiosk.Kiosk.Core.Sessions;

/// <summary>
/// Customer facts extracted from the scanned identity document, held in memory for the
/// transaction record only. Restricted PII: never journaled, never logged; written solely
/// into the legacy-shaped <c>transactionDetails.json</c> at settlement (ADR 0002).
/// </summary>
/// <param name="FirstName">Given name as printed on the document.</param>
/// <param name="LastName">Family name as printed on the document.</param>
/// <param name="DateOfBirth">The customer's date of birth.</param>
public sealed record CustomerFacts(string FirstName, string LastName, DateOnly DateOfBirth);
