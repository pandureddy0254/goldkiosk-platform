using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Events;
using GoldKiosk.Contracts.V1.Identity;
using GoldKiosk.Kiosk.UI.Resources;

namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// Maps stable machine-readable codes — <see cref="RejectionReasonCodes"/> and the
/// <see cref="ProblemTypes"/> registry — to localized titles, messages and recovery
/// buttons. Display text always resolves edge-side from the catalogue; server text is
/// only a fallback.
/// </summary>
public static class ErrorCatalog
{
    /// <summary>Resolves an API problem into a localized error presentation.</summary>
    /// <param name="problem">The parsed problem, when the response carried one.</param>
    /// <param name="strings">The string catalogue.</param>
    /// <returns>The localized presentation.</returns>
    public static ErrorPresentation ForProblem(KioskProblem? problem, ILocalizedStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        string code = problem?.Code ?? "generic";
        return Build(code, problem?.Detail, ChoicesFor(code, strings), strings);
    }

    /// <summary>Resolves an item/customer rejection into a localized error presentation.</summary>
    /// <param name="rejection">The rejection event payload.</param>
    /// <param name="strings">The string catalogue.</param>
    /// <returns>The localized presentation.</returns>
    public static ErrorPresentation ForRejection(RejectionDto rejection, ILocalizedStrings strings)
    {
        ArgumentNullException.ThrowIfNull(rejection);
        ArgumentNullException.ThrowIfNull(strings);
        return Build(
            rejection.ReasonCode,
            rejection.Display,
            [new RecoveryChoice(strings["error.action.ok"], RecoveryAction.Dismiss, IsPrimary: true)],
            strings);
    }

    /// <summary>Resolves a failed identity step into a localized error presentation.</summary>
    /// <param name="failure">The identity failure payload.</param>
    /// <param name="strings">The string catalogue.</param>
    /// <returns>The localized presentation.</returns>
    public static ErrorPresentation ForIdentityFailure(IdentityFailureDto failure, ILocalizedStrings strings)
    {
        ArgumentNullException.ThrowIfNull(failure);
        ArgumentNullException.ThrowIfNull(strings);

        IReadOnlyList<RecoveryChoice> choices = failure.RetriesLeft > 0
            ?
            [
                new RecoveryChoice(strings["error.action.retry"], RecoveryAction.Dismiss, IsPrimary: true),
                new RecoveryChoice(strings["error.action.return_item"], RecoveryAction.ReturnItem, IsPrimary: false),
            ]
            : [new RecoveryChoice(strings["error.action.ok"], RecoveryAction.Dismiss, IsPrimary: true)];

        ErrorPresentation presentation = Build(failure.ReasonCode, detail: null, choices, strings);
        return failure.RetriesLeft > 0
            ? presentation with
            {
                Message = presentation.Message + " " + strings.Format("error.identity.retries_left", failure.RetriesLeft),
            }
            : presentation;
    }

    /// <summary>Builds the presentation for a connectivity failure (API unreachable).</summary>
    /// <param name="strings">The string catalogue.</param>
    /// <returns>The localized presentation.</returns>
    public static ErrorPresentation ForConnectivity(ILocalizedStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);
        return new ErrorPresentation(
            strings["error.connectivity.title"],
            strings["error.connectivity.message"],
            [new RecoveryChoice(strings["error.action.ok"], RecoveryAction.Dismiss, IsPrimary: true)]);
    }

    private static ErrorPresentation Build(
        string code, string? detail, IReadOnlyList<RecoveryChoice> choices, ILocalizedStrings strings)
    {
        string titleKey = $"error.{code}.title";
        string messageKey = $"error.{code}.message";
        string title = strings.Contains(titleKey) ? strings[titleKey] : strings["error.generic.title"];
        string message = strings.Contains(messageKey)
            ? strings[messageKey]
            : detail ?? strings["error.generic.message"];
        return new ErrorPresentation(title, message, choices);
    }

    private static IReadOnlyList<RecoveryChoice> ChoicesFor(string code, ILocalizedStrings strings) =>
        code switch
        {
            "payout.insufficient_cash" =>
            [
                new RecoveryChoice(strings["error.action.choose_method"], RecoveryAction.Dismiss, IsPrimary: true),
                new RecoveryChoice(strings["error.action.return_item"], RecoveryAction.ReturnItem, IsPrimary: false),
            ],
            "payout.invalid_bank_details" or "tray.blocked" or "identity.step_failed" or "idempotency.key_conflict" =>
            [
                new RecoveryChoice(strings["error.action.retry"], RecoveryAction.Dismiss, IsPrimary: true),
            ],
            "session.not_found" or "session.expired" =>
            [
                new RecoveryChoice(strings["error.action.back_to_start"], RecoveryAction.BackToStart, IsPrimary: true),
            ],
            "offer.expired" =>
            [
                new RecoveryChoice(strings["error.action.ok"], RecoveryAction.Dismiss, IsPrimary: true),
                new RecoveryChoice(strings["error.action.return_item"], RecoveryAction.ReturnItem, IsPrimary: false),
            ],
            "settle.authorization_required" =>
            [
                new RecoveryChoice(strings["error.action.retry"], RecoveryAction.Dismiss, IsPrimary: true),
                new RecoveryChoice(strings["error.action.return_item"], RecoveryAction.ReturnItem, IsPrimary: false),
            ],
            _ =>
            [
                new RecoveryChoice(strings["error.action.ok"], RecoveryAction.Dismiss, IsPrimary: true),
            ],
        };
}
