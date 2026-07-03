using System.Globalization;
using System.Text.Json;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Kiosk.Core.Cloud;
using GoldKiosk.Kiosk.Core.Options;
using GoldKiosk.Kiosk.Core.Persistence;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Kiosk.Api.Cloud;

/// <summary>
/// The edge transaction outbox (ADR 0002): scans the transaction folders under the
/// <see cref="KioskOptions.TransactionRoot"/> for completed transactions not yet forwarded
/// (no <c>.uploaded</c> marker), forwards each to Cloud.Api idempotently via
/// <see cref="ICloudGateway.UploadTransactionAsync"/>, and marks it on success. Retries with
/// exponential backoff and never blocks the customer flow. Inert when
/// <c>Cloud:Enabled=false</c>.
/// </summary>
public sealed class TransactionUploadWorker : BackgroundService
{
    private const string JournalFileName = "journal.json";
    private const string UploadedMarker = ".uploaded";
    private const string SkippedMarker = ".skipped";
    private const double MaxBackoffSeconds = 300;

    private static readonly string[] _itemImagePrefixes =
        ["enteringItemImage", "exitingItemImage", "itemImageForLiveAgent"];

    private static readonly string[] _itemImageExtensions =
        [".png", ".jpg", ".jpeg", ".bmp", ".webp"];

    private readonly ICloudGateway _gateway;
    private readonly IOptions<CloudOptions> _cloudOptions;
    private readonly IOptions<KioskOptions> _kioskOptions;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TransactionUploadWorker> _logger;

    /// <summary>Initializes the worker.</summary>
    /// <param name="gateway">The cloud gateway.</param>
    /// <param name="cloudOptions">The validated cloud options.</param>
    /// <param name="kioskOptions">The kiosk options carrying the transaction root.</param>
    /// <param name="timeProvider">The clock (backoff + marker timestamps).</param>
    /// <param name="logger">The host logger.</param>
    public TransactionUploadWorker(
        ICloudGateway gateway,
        IOptions<CloudOptions> cloudOptions,
        IOptions<KioskOptions> kioskOptions,
        TimeProvider timeProvider,
        ILogger<TransactionUploadWorker> logger)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _cloudOptions = cloudOptions ?? throw new ArgumentNullException(nameof(cloudOptions));
        _kioskOptions = kioskOptions ?? throw new ArgumentNullException(nameof(kioskOptions));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_cloudOptions.Value.Enabled)
        {
            // Offline demo path: no cloud sync at all.
            return;
        }

        TimeSpan baseDelay = TimeSpan.FromSeconds(_cloudOptions.Value.UploadIntervalSeconds);
        TimeSpan delay = baseDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            bool hadFailure;
            try
            {
                hadFailure = await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Deliberate catch-all: a failed sweep must never stop the outbox — a stalled
                // worker would silently strand completed transactions on the machine.
                _logger.OutboxSweepFailed(ex);
                hadFailure = true;
            }

            delay = hadFailure
                ? TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, MaxBackoffSeconds))
                : baseDelay;

            try
            {
                await Task.Delay(delay, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task<bool> SweepAsync(CancellationToken cancellationToken)
    {
        string root = _kioskOptions.Value.TransactionRoot;
        if (!Directory.Exists(root))
        {
            return false;
        }

        bool hadFailure = false;
        foreach (string dayFolder in Directory.EnumerateDirectories(root))
        {
            foreach (string folder in Directory.EnumerateDirectories(dayFolder))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await TryForwardFolderAsync(folder, cancellationToken) == ForwardOutcome.Deferred)
                {
                    hadFailure = true;
                }
            }
        }

        return hadFailure;
    }

    private async Task<ForwardOutcome> TryForwardFolderAsync(string folder, CancellationToken cancellationToken)
    {
        if (File.Exists(Path.Combine(folder, UploadedMarker)) || File.Exists(Path.Combine(folder, SkippedMarker)))
        {
            return ForwardOutcome.AlreadyHandled;
        }

        string journalPath = Path.Combine(folder, JournalFileName);
        if (!File.Exists(journalPath))
        {
            return ForwardOutcome.AlreadyHandled;
        }

        string journalJson = await File.ReadAllTextAsync(journalPath, cancellationToken);
        SessionJournal? journal = TryParse(journalJson);
        if (journal is null)
        {
            // A torn or foreign journal: leave it for the next sweep / manual review.
            return ForwardOutcome.AlreadyHandled;
        }

        if (!string.Equals(journal.State, SessionStates.Done, StringComparison.Ordinal))
        {
            // Still in flight (or awaiting startup recovery): handled once it reaches a terminal state.
            return ForwardOutcome.AlreadyHandled;
        }

        if (!IsCompletedTransaction(journal))
        {
            // Terminal but not a completed transaction (aborted / returned / rejected):
            // nothing to forward. Mark skipped so the sweep does not revisit it.
            await WriteMarkerAsync(folder, SkippedMarker, cancellationToken);
            _logger.TransactionSkipped(journal.SessionId, "terminal but not a completed transaction");
            return ForwardOutcome.AlreadyHandled;
        }

        var upload = new TransactionUpload(
            journal.SessionId,
            folder,
            journalJson,
            CollectItemImages(folder));

        bool uploaded = await _gateway.UploadTransactionAsync(upload, cancellationToken);
        if (!uploaded)
        {
            _logger.TransactionUploadDeferred(journal.SessionId);
            return ForwardOutcome.Deferred;
        }

        await WriteMarkerAsync(folder, UploadedMarker, cancellationToken);
        return ForwardOutcome.Uploaded;
    }

    private static SessionJournal? TryParse(string journalJson)
    {
        try
        {
            return JsonSerializer.Deserialize<SessionJournal>(journalJson, CloudJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsCompletedTransaction(SessionJournal journal) =>
        journal.Receipt is not null
        && journal.Offer is not null
        && journal.Offer.Amount.AmountMinor > 0;

    private static List<string> CollectItemImages(string folder)
    {
        // Only non-PII item images are ever enumerated; selfies, signatures, ID and
        // fingerprint images stay on the machine (security-standards PII rules).
        List<string> images = [];
        foreach (string path in Directory.EnumerateFiles(folder))
        {
            string name = Path.GetFileName(path);
            string extension = Path.GetExtension(name);
            bool isItemImage = _itemImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)
                && _itemImagePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (isItemImage)
            {
                images.Add(path);
            }
        }

        return images;
    }

    private async Task WriteMarkerAsync(string folder, string markerName, CancellationToken cancellationToken)
    {
        string stamp = _timeProvider.GetUtcNow().ToString("O", CultureInfo.InvariantCulture);
        await File.WriteAllTextAsync(Path.Combine(folder, markerName), stamp, cancellationToken);
    }

    private enum ForwardOutcome
    {
        AlreadyHandled,
        Uploaded,
        Deferred,
    }
}
