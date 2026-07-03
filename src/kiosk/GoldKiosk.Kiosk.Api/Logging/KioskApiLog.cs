using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Api.Logging;

/// <summary>
/// Source-generated log messages for the Kiosk.Api host (LoggerMessage delegates per the
/// coding standard). Messages carry identifiers only — never PII, never secrets.
/// </summary>
internal static partial class KioskApiLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information,
        Message = "Session {SessionId} began (is_test: {IsTest})")]
    public static partial void SessionBegan(this ILogger logger, string sessionId, bool isTest);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Warning,
        Message = "Session {SessionId} aborted (reason: {Reason})")]
    public static partial void SessionAborted(this ILogger logger, string sessionId, string reason);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
        Message = "Session {SessionId}: offer {OfferId} presented ({Display})")]
    public static partial void OfferPresented(this ILogger logger, string sessionId, string offerId, string display);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning,
        Message = "Session {SessionId}: could not present offer ({Code})")]
    public static partial void OfferPresentFailed(this ILogger logger, string sessionId, string code);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information,
        Message = "Session {SessionId} completed; receipt {ReceiptId} (is_test: {IsTest})")]
    public static partial void SessionCompleted(this ILogger logger, string sessionId, string receiptId, bool isTest);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Warning,
        Message = "Session {SessionId}: could not complete ({Code})")]
    public static partial void SessionCompleteFailed(this ILogger logger, string sessionId, string code);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Information,
        Message = "Session {SessionId}: item rejected ({ReasonCode})")]
    public static partial void ItemRejected(this ILogger logger, string sessionId, string reasonCode);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Warning,
        Message = "Session {SessionId}: rejection {ReasonCode} not applicable in state {State}")]
    public static partial void RejectionNotApplicable(
        this ILogger logger, string sessionId, string reasonCode, string state);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Warning,
        Message = "Session {SessionId}: tray-closed sensor did not confirm; aborting")]
    public static partial void TraySensorNotConfirmed(this ILogger logger, string sessionId);

    [LoggerMessage(EventId = 1009, Level = LogLevel.Error,
        Message = "Background {Operation} failed for session {SessionId}")]
    public static partial void BackgroundOperationFailed(
        this ILogger logger, Exception exception, string operation, string sessionId);

    [LoggerMessage(EventId = 1010, Level = LogLevel.Error,
        Message = "Safe-abort failed for session {SessionId}")]
    public static partial void SafeAbortFailed(this ILogger logger, Exception exception, string sessionId);

    [LoggerMessage(EventId = 1011, Level = LogLevel.Warning,
        Message = "Failed publishing {EventName} for session {SessionId}")]
    public static partial void EventPublishFailed(
        this ILogger logger, Exception exception, string eventName, string sessionId);

    [LoggerMessage(EventId = 2000, Level = LogLevel.Information,
        Message = "Device {DeviceKey} composed in {Mode} mode ({DisplayName}, critical: {IsCritical})")]
    public static partial void DeviceComposed(
        this ILogger logger, string deviceKey, DeviceMode mode, string displayName, bool isCritical);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning,
        Message = "One or more devices are mocked — every session on this kiosk is flagged is_test (ADR 0004)")]
    public static partial void MockedDevicesInUse(this ILogger logger);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information,
        Message = "Device {DeviceKey} connected: {State} {Detail}")]
    public static partial void DeviceConnected(
        this ILogger logger, string deviceKey, DeviceState state, string? detail);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning,
        Message = "Device {DeviceKey} failed to disconnect cleanly")]
    public static partial void DeviceDisconnectFailed(this ILogger logger, Exception exception, string deviceKey);

    [LoggerMessage(EventId = 3000, Level = LogLevel.Information,
        Message = "Startup recovery: no interrupted sessions found")]
    public static partial void RecoveryNoneFound(this ILogger logger);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning,
        Message = "Startup recovery: session {SessionId} was interrupted in state {State} — safe-aborting")]
    public static partial void RecoveryInterruptedSession(this ILogger logger, string sessionId, string state);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Warning,
        Message = "Startup recovery: safe-aborted {Count} interrupted session(s)")]
    public static partial void RecoverySafeAborted(this ILogger logger, int count);

    [LoggerMessage(EventId = 4000, Level = LogLevel.Warning,
        Message = "Session {SessionId} idle for {IdleSeconds}s — auto-aborting (timeout)")]
    public static partial void IdleTimeoutAborting(this ILogger logger, string sessionId, int idleSeconds);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Error,
        Message = "Idle watchdog sweep failed; the watchdog keeps running")]
    public static partial void IdleWatchdogSweepFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 5000, Level = LogLevel.Error,
        Message = "Unhandled exception on {Path}")]
    public static partial void UnhandledException(this ILogger logger, Exception exception, string path);
}
