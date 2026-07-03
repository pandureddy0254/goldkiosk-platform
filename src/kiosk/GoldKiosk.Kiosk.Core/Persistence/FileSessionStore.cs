using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Domain.ValueObjects;
using GoldKiosk.Kiosk.Core.Options;
using GoldKiosk.Kiosk.Core.Sessions;

namespace GoldKiosk.Kiosk.Core.Persistence;

/// <summary>
/// File-based <see cref="ISessionStore"/> per ADR 0002: one folder per transaction under
/// <c>{root}/{dd-MM-yyyy}/{HH-mm-ss}/</c>, <c>journal.json</c> rewritten via
/// write-temp-then-replace on every transition, <c>transactionLog.txt</c> appended per
/// step, and the legacy-shaped <c>transactionDetails.json</c> written at settlement.
/// No SQL runs on the kiosk.
/// </summary>
public sealed class FileSessionStore : ISessionStore
{
    private const string JournalFileName = "journal.json";
    private const string LogFileName = "transactionLog.txt";
    private const string DetailsFileName = "transactionDetails.json";

    private static readonly JsonSerializerOptions _journalSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };

    private static readonly JsonSerializerOptions _legacySerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly KioskOptions _options;
    private readonly TimeProvider _timeProvider;

    // One gate per transaction folder: concurrent persists for the same session would
    // otherwise race on the journal replace. Entries live for the process lifetime,
    // matching the session registry's retention of terminal sessions.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _persistGates =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Initializes the store.</summary>
    /// <param name="options">The kiosk options carrying the transaction root and identity.</param>
    /// <param name="timeProvider">Time source for folder names and log timestamps.</param>
    public FileSessionStore(KioskOptions options, TimeProvider timeProvider)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc />
    public async Task CreateTransactionFolderAsync(
        TransactionSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        DateTimeOffset now = _timeProvider.GetLocalNow();
        string day = now.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
        string time = now.ToString("HH-mm-ss", CultureInfo.InvariantCulture);

        string folder = Path.Combine(_options.TransactionRoot, day, time);
        int suffix = 2;
        while (Directory.Exists(folder))
        {
            folder = Path.Combine(_options.TransactionRoot, day, $"{time}-{suffix++}");
        }

        Directory.CreateDirectory(folder);
        session.AttachFolder(folder);

        await PersistAsync(session, cancellationToken).ConfigureAwait(false);
        await AppendLogAsync(session, $"Transaction folder created for session {session.Id}.", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task PersistAsync(TransactionSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.TransactionFolder is not string folder)
        {
            return;
        }

        SemaphoreSlim gate = _persistGates.GetOrAdd(folder, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string json = JsonSerializer.Serialize(SessionJournal.FromSession(session), _journalSerializerOptions);
            await WriteAtomicAsync(Path.Combine(folder, JournalFileName), json, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task AppendLogAsync(
        TransactionSession session,
        string message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        if (session.TransactionFolder is null)
        {
            return;
        }

        string stamp = _timeProvider.GetLocalNow().ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
        string line = $"[{stamp}] {message}{Environment.NewLine}";
        await File.AppendAllTextAsync(Path.Combine(session.TransactionFolder, LogFileName), line, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task WriteTransactionDetailsAsync(
        TransactionSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.TransactionFolder is null)
        {
            return;
        }

        string json = JsonSerializer.Serialize(BuildLegacyDetails(session), _legacySerializerOptions);
        await WriteAtomicAsync(Path.Combine(session.TransactionFolder, DetailsFileName), json, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task WriteArtifactAsync(
        TransactionSession session,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(content);
        if (fileName != Path.GetFileName(fileName))
        {
            throw new ArgumentException("Artifact file name must not contain path separators.", nameof(fileName));
        }

        if (session.TransactionFolder is null)
        {
            return;
        }

        await File.WriteAllBytesAsync(Path.Combine(session.TransactionFolder, fileName), content, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecoveredSession>> RecoverCurrentDayAsync(
        CancellationToken cancellationToken = default)
    {
        // Today and yesterday: a session interrupted just before midnight would otherwise
        // never be found by a restart in the small hours.
        DateTimeOffset now = _timeProvider.GetLocalNow();
        List<RecoveredSession> recovered = [];
        foreach (DateTimeOffset date in (DateTimeOffset[])[now.AddDays(-1), now])
        {
            string day = date.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
            string dayFolder = Path.Combine(_options.TransactionRoot, day);
            if (!Directory.Exists(dayFolder))
            {
                continue;
            }

            foreach (string folder in Directory.EnumerateDirectories(dayFolder))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string journalPath = Path.Combine(folder, JournalFileName);
                if (!File.Exists(journalPath))
                {
                    continue;
                }

                try
                {
                    string json = await File.ReadAllTextAsync(journalPath, cancellationToken).ConfigureAwait(false);
                    SessionJournal? journal = JsonSerializer.Deserialize<SessionJournal>(json, _journalSerializerOptions);
                    if (journal is not null && journal.State != SessionStates.Done)
                    {
                        recovered.Add(new RecoveredSession(folder, journal));
                    }
                }
                catch (JsonException)
                {
                    // A torn or foreign journal: skip it here — the folder stays on disk for
                    // manual review, and Diagnostics reports unreadable journals separately.
                }
            }
        }

        return recovered;
    }

    /// <inheritdoc />
    public async Task SafeAbortAsync(RecoveredSession recovered, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recovered);

        SessionJournal aborted = recovered.Journal with
        {
            State = SessionStates.Done,
            AbortReason = "fault",
        };
        string json = JsonSerializer.Serialize(aborted, _journalSerializerOptions);
        await WriteAtomicAsync(Path.Combine(recovered.FolderPath, JournalFileName), json, cancellationToken)
            .ConfigureAwait(false);

        string stamp = _timeProvider.GetLocalNow().ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture);
        string line = $"[{stamp}] Session {aborted.SessionId} was interrupted (last state "
            + $"'{recovered.Journal.State}') and safe-aborted at startup recovery.{Environment.NewLine}";
        await File.AppendAllTextAsync(Path.Combine(recovered.FolderPath, LogFileName), line, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task WriteAtomicAsync(string path, string content, CancellationToken cancellationToken)
    {
        // Unique temp name per write: a shared "<file>.tmp" would race when two writes
        // for the same target ever overlap despite the per-session gate.
        string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        await File.WriteAllTextAsync(tempPath, content, cancellationToken).ConfigureAwait(false);
        File.Move(tempPath, path, overwrite: true);
    }

    private LegacyTransactionDetails BuildLegacyDetails(TransactionSession session)
    {
        decimal weightGrams = session.Analysis?.WeightGrams ?? 0m;
        decimal goldPercent = session.Analysis?.GoldPercent ?? 0m;
        decimal silverPercent = session.Analysis?.SilverPercent ?? 0m;
        bool offerAccepted = session.Payout is not null;

        decimal offerMajor = 0m;
        if (session.Offer is not null)
        {
            offerMajor = Money
                .FromMinorUnits(session.Offer.Amount.AmountMinor, session.Offer.Amount.Currency)
                .Amount;
        }

        decimal payoutMajor = offerAccepted ? offerMajor : 0m;

        var item = new LegacyItem
        {
            Temperature = 0m,
            Weight = weightGrams,
            DwtWeight = weightGrams == 0m
                ? 0m
                : decimal.Round(GoldWeight.FromGrams(weightGrams).ToDwt(), 3, MidpointRounding.ToEven),
            Karat = decimal.Round(goldPercent / 100m * 24m, 1, MidpointRounding.ToEven),
            MetalPercentage = goldPercent,
            MarketPrice = 0m,
            TierPricePerDwt = 0m,
            UnitPricePerDwt = 0m,
            Impurities = string.Empty,
            OfferPrice = offerMajor,
            OfferAccepted = offerAccepted,
            OfferId = session.Offer?.OfferId ?? string.Empty,
            Payout = payoutMajor,
            // Metal type is only decidable once an analysis reading exists with a
            // non-zero composition — never default to Gold on empty data.
            MetalType = session.Analysis is null || (goldPercent == 0m && silverPercent == 0m)
                ? string.Empty
                : goldPercent >= silverPercent ? "Gold" : "Silver",
            BagNumber = session.BagNumber ?? string.Empty,
            ImageProcessingDir = string.Empty,
            OfferMadeOn = session.OfferMadeAt,
            CustomerRespondedToOfferOn = session.OfferActionedAt,
        };

        return new LegacyTransactionDetails
        {
            StoreTransactionId = Guid.CreateVersion7(_timeProvider.GetUtcNow()),
            DispenserId = _options.KioskId,
            StoreId = _options.StoreId,
            LicenseNo = string.Empty,
            Customer = new LegacyCustomer
            {
                Phone = session.Contact?.Phone ?? string.Empty,
                First = session.Customer?.FirstName ?? string.Empty,
                Last = session.Customer?.LastName ?? string.Empty,
                Dob = session.Customer?.DateOfBirth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                    ?? string.Empty,
                Email = session.Contact?.Email ?? string.Empty,
            },
            FingerPrintHash = string.Empty,
            IsRefund = false,
            Items = [item],
            TotalPayout = payoutMajor,
            CashDetails = LegacyCashDetails.FromBills(session.Payout?.PlannedBills),
            IsPawn = string.Equals(session.Setup?.ServiceType, "pawn", StringComparison.Ordinal),
            IsCrypto = false,
            CryptoCurrencySelected = null,
        };
    }
}
