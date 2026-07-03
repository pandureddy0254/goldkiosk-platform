namespace GoldKiosk.Contracts.V1.Tray;

/// <summary>
/// The customer's pre-tray selections, shipped in one batch with the tray-open command.
/// </summary>
/// <param name="ServiceType">The chosen service: <c>sell</c> or <c>pawn</c>.</param>
/// <param name="Locale">The BCP 47 locale in effect, e.g. <c>en-US</c>.</param>
/// <param name="Terms">The inline terms acceptance.</param>
/// <param name="ItemHint">An optional item hint chip (AI auto-detects; no description questions).</param>
/// <param name="PromoCode">An optional promotional code.</param>
public sealed record SetupDto(
    string ServiceType,
    string Locale,
    TermsAcceptanceDto Terms,
    string? ItemHint,
    string? PromoCode);
