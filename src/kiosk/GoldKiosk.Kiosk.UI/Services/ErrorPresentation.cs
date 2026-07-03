namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>A resolved, localized error ready for the error overlay.</summary>
/// <param name="Title">The localized title.</param>
/// <param name="Message">The localized customer-facing message.</param>
/// <param name="Choices">The recovery buttons, primary first.</param>
public sealed record ErrorPresentation(
    string Title,
    string Message,
    IReadOnlyList<RecoveryChoice> Choices);
