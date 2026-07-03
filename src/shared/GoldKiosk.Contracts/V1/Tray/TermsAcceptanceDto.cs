namespace GoldKiosk.Contracts.V1.Tray;

/// <summary>
/// The customer's terms-and-conditions acceptance, captured inline on the place-item screen.
/// </summary>
/// <param name="Version">The terms version presented, e.g. <c>2026-06-01.v3</c>.</param>
/// <param name="Accepted">Whether the customer ticked the acceptance checkbox.</param>
/// <param name="AcceptedAt">When the acceptance was captured.</param>
public sealed record TermsAcceptanceDto(string Version, bool Accepted, DateTimeOffset AcceptedAt);
