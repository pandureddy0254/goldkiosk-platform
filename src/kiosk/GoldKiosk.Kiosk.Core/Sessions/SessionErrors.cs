using GoldKiosk.Domain.Primitives;

namespace GoldKiosk.Kiosk.Core.Sessions;

/// <summary>
/// Factory for the session-level <see cref="DomainError"/>s. Codes are the suffixes of the
/// ProblemDetails <c>type</c> URIs in <c>GoldKiosk.Contracts.V1.Common.ProblemTypes</c>, so
/// the API host maps a failure to its wire type by appending the code to the problem base URI.
/// </summary>
public static class SessionErrors
{
    /// <summary>The requested action is not valid in the session's current state.</summary>
    /// <param name="action">The attempted action, for the human-readable message.</param>
    /// <param name="state">The session's current state.</param>
    /// <returns>The domain error.</returns>
    public static DomainError InvalidState(string action, string state) =>
        new("session.invalid_state", $"Cannot {action} while the session is in state '{state}'.");

    /// <summary>The offer's lock TTL has elapsed.</summary>
    /// <returns>The domain error.</returns>
    public static DomainError OfferExpired() =>
        new("offer.expired", "The offer has expired; the price lock is no longer valid.");

    /// <summary>The offer was already accepted or declined.</summary>
    /// <returns>The domain error.</returns>
    public static DomainError OfferAlreadyActioned() =>
        new("offer.already_actioned", "The offer was already accepted or declined.");

    /// <summary>The cassettes cannot cover the payout amount in cash.</summary>
    /// <returns>The domain error.</returns>
    public static DomainError InsufficientCash() =>
        new("payout.insufficient_cash", "Cash is unavailable for this amount.");
}
