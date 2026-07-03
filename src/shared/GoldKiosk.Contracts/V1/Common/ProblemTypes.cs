namespace GoldKiosk.Contracts.V1.Common;

/// <summary>
/// Stable RFC 7807 ProblemDetails <c>type</c> URIs for the Kiosk API.
/// Values are additive within a contract version; never repurpose a code.
/// </summary>
public static class ProblemTypes
{
    private const string Base = "https://goldkiosk.dev/problems/";

    /// <summary>The session id is unknown to this kiosk.</summary>
    public const string SessionNotFound = Base + "session.not_found";

    /// <summary>The requested action is not valid in the session's current state.</summary>
    public const string SessionInvalidState = Base + "session.invalid_state";

    /// <summary>The session has expired (idle timeout or lifetime exceeded).</summary>
    public const string SessionExpired = Base + "session.expired";

    /// <summary>The tray is physically blocked and cannot move.</summary>
    public const string TrayBlocked = Base + "tray.blocked";

    /// <summary>The tray hardware reported a fault.</summary>
    public const string TrayHardwareFault = Base + "tray.hardware_fault";

    /// <summary>The offer's lock TTL has elapsed.</summary>
    public const string OfferExpired = Base + "offer.expired";

    /// <summary>The offer was already accepted or declined.</summary>
    public const string OfferAlreadyActioned = Base + "offer.already_actioned";

    /// <summary>An identity step failed and can be retried.</summary>
    public const string IdentityStepFailed = Base + "identity.step_failed";

    /// <summary>An identity step failed with no retries remaining.</summary>
    public const string IdentityRetriesExhausted = Base + "identity.retries_exhausted";

    /// <summary>The cassettes cannot cover the payout amount in cash.</summary>
    public const string PayoutInsufficientCash = Base + "payout.insufficient_cash";

    /// <summary>The submitted bank details failed validation.</summary>
    public const string PayoutInvalidBankDetails = Base + "payout.invalid_bank_details";

    /// <summary>Settlement requires a cloud authorization record that is not present.</summary>
    public const string SettleAuthorizationRequired = Base + "settle.authorization_required";

    /// <summary>Settlement hardware (bagger/dispenser) reported a fault.</summary>
    public const string SettleHardwareFault = Base + "settle.hardware_fault";

    /// <summary>A required device is unavailable.</summary>
    public const string DeviceUnavailable = Base + "device.unavailable";

    /// <summary>The idempotency key was reused with a different request payload.</summary>
    public const string IdempotencyKeyConflict = Base + "idempotency.key_conflict";
}
