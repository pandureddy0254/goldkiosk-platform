using Microsoft.Extensions.Logging;

namespace GoldKiosk.Kiosk.UI.Logging;

/// <summary>
/// Source-generated log messages for the kiosk UI shell (LoggerMessage delegates per the
/// coding standard). Messages carry identifiers only — never PII, never secrets.
/// </summary>
internal static partial class KioskUiLog
{
    [LoggerMessage(EventId = 2000, Level = LogLevel.Information,
        Message = "Kiosk UI starting; API base {ApiBaseUrl}")]
    public static partial void UiStarting(this ILogger logger, string apiBaseUrl);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Kiosk UI exiting with code {ExitCode}")]
    public static partial void UiExiting(this ILogger logger, int exitCode);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Error,
        Message = "Unhandled dispatcher exception; kiosk kept alive")]
    public static partial void DispatcherException(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Critical,
        Message = "Unhandled AppDomain exception (terminating: {IsTerminating})")]
    public static partial void DomainException(this ILogger logger, Exception? exception, bool isTerminating);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Error,
        Message = "Unobserved task exception; kiosk kept alive")]
    public static partial void UnobservedTaskException(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2010, Level = LogLevel.Information,
        Message = "Kiosk hub connected")]
    public static partial void HubConnected(this ILogger logger);

    [LoggerMessage(EventId = 2011, Level = LogLevel.Warning,
        Message = "Kiosk hub connect failed; retrying")]
    public static partial void HubConnectRetrying(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2012, Level = LogLevel.Information,
        Message = "Kiosk hub reconnected")]
    public static partial void HubReconnected(this ILogger logger);

    [LoggerMessage(EventId = 2013, Level = LogLevel.Warning,
        Message = "Kiosk hub connection closed")]
    public static partial void HubClosed(this ILogger logger, Exception? exception);

    [LoggerMessage(EventId = 2014, Level = LogLevel.Error,
        Message = "Kiosk hub connection could not be started")]
    public static partial void HubStartFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2020, Level = LogLevel.Warning,
        Message = "Kiosk API {Method} {Path} returned {StatusCode} ({ProblemCode})")]
    public static partial void ApiProblem(
        this ILogger logger, string method, string? path, int statusCode, string problemCode);

    [LoggerMessage(EventId = 2021, Level = LogLevel.Error,
        Message = "Kiosk API call failed")]
    public static partial void ApiCallFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2030, Level = LogLevel.Warning,
        Message = "Session resync failed for {SessionId}")]
    public static partial void ResyncFailed(this ILogger logger, Exception exception, string sessionId);

    [LoggerMessage(EventId = 2031, Level = LogLevel.Error,
        Message = "Abort ({Reason}) failed for session {SessionId}")]
    public static partial void AbortFailed(this ILogger logger, Exception exception, string reason, string sessionId);

    [LoggerMessage(EventId = 2032, Level = LogLevel.Error,
        Message = "Settle failed for session {SessionId}")]
    public static partial void SettleFailed(this ILogger logger, Exception exception, string sessionId);

    [LoggerMessage(EventId = 2040, Level = LogLevel.Information,
        Message = "DEBUG exit gesture (Escape+F12) — shutting down")]
    public static partial void DebugExitGesture(this ILogger logger);

    [LoggerMessage(EventId = 2050, Level = LogLevel.Information,
        Message = "Local Kiosk API already answering at {ApiBaseUrl}; not launching")]
    public static partial void LocalApiAlreadyRunning(this ILogger logger, string apiBaseUrl);

    [LoggerMessage(EventId = 2051, Level = LogLevel.Error,
        Message = "Local Kiosk API executable not found at {ExePath}")]
    public static partial void LocalApiExecutableMissing(this ILogger logger, string exePath);

    [LoggerMessage(EventId = 2052, Level = LogLevel.Information,
        Message = "Launching local Kiosk API: {ExePath}")]
    public static partial void LocalApiStarting(this ILogger logger, string exePath);

    [LoggerMessage(EventId = 2053, Level = LogLevel.Information,
        Message = "Local Kiosk API healthy at {ApiBaseUrl}")]
    public static partial void LocalApiHealthy(this ILogger logger, string apiBaseUrl);

    [LoggerMessage(EventId = 2054, Level = LogLevel.Warning,
        Message = "Local Kiosk API did not become healthy at {ApiBaseUrl} within the wait window")]
    public static partial void LocalApiHealthTimedOut(this ILogger logger, string apiBaseUrl);
}
