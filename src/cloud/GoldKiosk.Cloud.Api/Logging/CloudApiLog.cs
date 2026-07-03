namespace GoldKiosk.Cloud.Api.Logging;

/// <summary>
/// Source-generated log messages for the Cloud.Api host (LoggerMessage delegates per the
/// coding standard). Messages carry identifiers and amounts only — never PII, never secrets.
/// </summary>
internal static partial class CloudApiLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Warning,
        Message = "Kiosk login rejected for code {KioskCode}")]
    public static partial void KioskLoginRejected(this ILogger logger, string kioskCode);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information,
        Message = "Kiosk {KioskCode} ({KioskId}) logged in")]
    public static partial void KioskLoggedIn(this ILogger logger, string kioskCode, Guid kioskId);

    [LoggerMessage(EventId = 2000, Level = LogLevel.Warning,
        Message = "GoldApi:ApiKey is not configured — skipping live rate fetch")]
    public static partial void RateFetchSkippedNoApiKey(this ILogger logger);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "GoldAPI.io fetched ({Currency}/g): 24K={Gold24k}, 22K={Gold22k}, 18K={Gold18k}, Ag={Silver}")]
    public static partial void RatesFetched(
        this ILogger logger, string currency, decimal gold24k, decimal gold22k, decimal gold18k, decimal silver);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Error,
        Message = "Failed to fetch rates from GoldAPI.io — degrading to last-known DB rates")]
    public static partial void RateFetchFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Information,
        Message = "GoldRateSyncService is disabled (GoldApi:EnableSync=false) — skipping")]
    public static partial void RateSyncDisabled(this ILogger logger);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information,
        Message = "GoldRateSyncService started (interval: {IntervalHours}h)")]
    public static partial void RateSyncStarted(this ILogger logger, int intervalHours);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information,
        Message = "Next rate sync in {Minutes} min")]
    public static partial void RateSyncSleeping(this ILogger logger, int minutes);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Information,
        Message = "Rates already synced by another instance — skipping fetch")]
    public static partial void RateSyncAlreadyFresh(this ILogger logger);

    [LoggerMessage(EventId = 2007, Level = LogLevel.Information,
        Message = "Saved {Count} metal rate rows (valid until {ValidUntil:u})")]
    public static partial void RateSyncSaved(this ILogger logger, int count, DateTimeOffset validUntil);

    [LoggerMessage(EventId = 2008, Level = LogLevel.Error,
        Message = "GoldRateSyncService encountered an error — retrying in {RetryMinutes} min")]
    public static partial void RateSyncError(this ILogger logger, Exception exception, int retryMinutes);

    [LoggerMessage(EventId = 2009, Level = LogLevel.Warning,
        Message = "Rate fetch returned no data — keeping existing DB rates")]
    public static partial void RateFetchReturnedNull(this ILogger logger);

    [LoggerMessage(EventId = 3000, Level = LogLevel.Information,
        Message = "Offer {OfferId} computed: {Display} for {Summary}")]
    public static partial void OfferComputed(this ILogger logger, Guid offerId, string display, string summary);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning,
        Message = "No usable rate for {Metal}/{Karat}K in {Currency} — offer refused")]
    public static partial void OfferRatesMissing(this ILogger logger, string metal, decimal karat, string currency);

    [LoggerMessage(EventId = 4000, Level = LogLevel.Warning,
        Message = "goldkiosk-ai {Operation} transport failure — degrading open per policy")]
    public static partial void AiTransportFailure(this ILogger logger, Exception exception, string operation);

    [LoggerMessage(EventId = 4001, Level = LogLevel.Warning,
        Message = "goldkiosk-ai {Operation} returned HTTP {StatusCode}")]
    public static partial void AiRequestFailed(this ILogger logger, string operation, int statusCode);

    [LoggerMessage(EventId = 4002, Level = LogLevel.Information,
        Message = "Item analyzed: status {Status}, label {Label}, confidence {Confidence}, live agent: {RequiresLiveAgent}")]
    public static partial void ItemAnalyzed(
        this ILogger logger, string status, string label, double confidence, bool requiresLiveAgent);

    [LoggerMessage(EventId = 4003, Level = LogLevel.Warning,
        Message = "Item analysis degraded open on AI transport failure (accepting item, per design)")]
    public static partial void ItemAnalysisDegradedOpen(this ILogger logger);

    [LoggerMessage(EventId = 5000, Level = LogLevel.Information,
        Message = "Live-agent item review {ReviewId} created for transaction {TransactionRef}")]
    public static partial void ReviewCreated(this ILogger logger, Guid reviewId, string transactionRef);

    [LoggerMessage(EventId = 5001, Level = LogLevel.Warning,
        Message = "Live-agent item review {ReviewId} timed out with no verdict — returning item, never auto-approving")]
    public static partial void ReviewTimedOut(this ILogger logger, Guid reviewId);

    [LoggerMessage(EventId = 6000, Level = LogLevel.Information,
        Message = "Transaction {TransactionId} recorded as {TransactionCode}")]
    public static partial void TransactionRecorded(this ILogger logger, Guid transactionId, string transactionCode);

    [LoggerMessage(EventId = 6001, Level = LogLevel.Information,
        Message = "Transaction {TransactionId} replayed idempotently")]
    public static partial void TransactionReplayed(this ILogger logger, Guid transactionId);

    [LoggerMessage(EventId = 9000, Level = LogLevel.Error,
        Message = "Unhandled exception on {Path}")]
    public static partial void UnhandledException(this ILogger logger, Exception exception, string path);
}
