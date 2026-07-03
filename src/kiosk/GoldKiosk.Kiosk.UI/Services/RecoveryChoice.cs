namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>One recovery button on the error overlay.</summary>
/// <param name="Label">The localized button label.</param>
/// <param name="Action">What pressing the button does.</param>
/// <param name="IsPrimary">Whether the button renders in the primary (gradient) style.</param>
public sealed record RecoveryChoice(string Label, RecoveryAction Action, bool IsPrimary);
