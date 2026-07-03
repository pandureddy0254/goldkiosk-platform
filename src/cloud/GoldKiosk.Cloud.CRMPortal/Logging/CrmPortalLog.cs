namespace GoldKiosk.Cloud.CRMPortal.Logging;

/// <summary>
/// Source-generated log messages for the CRM portal (LoggerMessage delegates per the
/// coding standard). Messages carry identifiers only — never PII (no email addresses),
/// never secrets or cleartext license tokens.
/// </summary>
internal static partial class CrmPortalLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information,
        Message = "License issued for partner {PartnerId} tenant {TenantId} key prefix {KeyPrefix}")]
    public static partial void LicenseIssued(
        this ILogger logger, Guid partnerId, Guid tenantId, string keyPrefix);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "SES sent license email for partner {PartnerId} tenant {TenantId}, message_id={MessageId}")]
    public static partial void LicenseEmailSent(
        this ILogger logger, Guid partnerId, Guid tenantId, string? messageId);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error,
        Message = "SES send failed for partner {PartnerId} tenant {TenantId}")]
    public static partial void LicenseEmailSendFailed(
        this ILogger logger, Exception exception, Guid partnerId, Guid tenantId);

    [LoggerMessage(EventId = 2000, Level = LogLevel.Error,
        Message = "verify_login RPC failed during a sign-in attempt")]
    public static partial void SignInVerificationFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Error,
        Message = "Password change failed for user {UserId}")]
    public static partial void PasswordChangeFailed(this ILogger logger, Exception exception, Guid userId);

    [LoggerMessage(EventId = 3000, Level = LogLevel.Error,
        Message = "cancel_subscription failed for subscription {SubscriptionId}")]
    public static partial void SubscriptionCancelFailed(
        this ILogger logger, Exception exception, Guid subscriptionId);
}
