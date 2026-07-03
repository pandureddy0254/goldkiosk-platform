using System.Net;

namespace GoldKiosk.Kiosk.Api.Cloud;

/// <summary>
/// Source-generated log messages for the cloud gateway, provisioning service and outbox
/// worker (LoggerMessage delegates per the coding standard). Messages carry identifiers
/// only — never PII, never secrets (never the PIN, never the token).
/// </summary>
internal static partial class CloudLog
{
    [LoggerMessage(EventId = 6000, Level = LogLevel.Information,
        Message = "Cloud integration disabled; kiosk runs on local mock pricing (offline mode)")]
    public static partial void CloudDisabled(this ILogger logger);

    [LoggerMessage(EventId = 6001, Level = LogLevel.Information,
        Message = "Kiosk authenticated to cloud (kiosk {KioskId})")]
    public static partial void KioskLoggedIn(this ILogger logger, Guid kioskId);

    [LoggerMessage(EventId = 6002, Level = LogLevel.Warning,
        Message = "Cloud kiosk-login was rejected — verify Cloud:KioskCode and the configured PIN")]
    public static partial void LoginRejected(this ILogger logger);

    [LoggerMessage(EventId = 6003, Level = LogLevel.Warning,
        Message = "Cloud {Operation} unreachable; the edge is degrading gracefully")]
    public static partial void CloudUnreachable(this ILogger logger, string operation);

    [LoggerMessage(EventId = 6004, Level = LogLevel.Warning,
        Message = "Cloud {Operation} returned {StatusCode}")]
    public static partial void CloudRequestFailed(this ILogger logger, string operation, HttpStatusCode statusCode);

    [LoggerMessage(EventId = 6005, Level = LogLevel.Error,
        Message = "Cloud {Operation} failed unexpectedly; treated as unavailable")]
    public static partial void CloudError(this ILogger logger, Exception exception, string operation);

    [LoggerMessage(EventId = 6006, Level = LogLevel.Information,
        Message = "Provisioning applied (kiosk {KioskId}, is_live {IsLive})")]
    public static partial void ProvisioningApplied(this ILogger logger, Guid kioskId, bool isLive);

    [LoggerMessage(EventId = 6007, Level = LogLevel.Warning,
        Message = "Cloud unreachable at startup; using last-good provisioning cache (kiosk {KioskId})")]
    public static partial void ProvisioningFromCache(this ILogger logger, Guid kioskId);

    [LoggerMessage(EventId = 6008, Level = LogLevel.Warning,
        Message = "No provisioning from cloud and no cache; kiosk starts unprovisioned (Diagnostics will surface this)")]
    public static partial void ProvisioningUnavailable(this ILogger logger);

    [LoggerMessage(EventId = 6009, Level = LogLevel.Warning,
        Message = "Failed reading/writing the provisioning cache")]
    public static partial void ProvisioningCacheIoFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 6010, Level = LogLevel.Information,
        Message = "Transaction {SessionId} forwarded to cloud")]
    public static partial void TransactionUploaded(this ILogger logger, string sessionId);

    [LoggerMessage(EventId = 6011, Level = LogLevel.Warning,
        Message = "Transaction {SessionId} upload deferred; will retry with backoff")]
    public static partial void TransactionUploadDeferred(this ILogger logger, string sessionId);

    [LoggerMessage(EventId = 6012, Level = LogLevel.Information,
        Message = "Transaction folder {SessionId} skipped ({Reason})")]
    public static partial void TransactionSkipped(this ILogger logger, string sessionId, string reason);

    [LoggerMessage(EventId = 6013, Level = LogLevel.Error,
        Message = "Transaction outbox sweep failed; the worker keeps running")]
    public static partial void OutboxSweepFailed(this ILogger logger, Exception exception);
}
