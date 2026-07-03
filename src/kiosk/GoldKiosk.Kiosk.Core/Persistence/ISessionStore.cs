using GoldKiosk.Kiosk.Core.Sessions;

namespace GoldKiosk.Kiosk.Core.Persistence;

/// <summary>
/// Port for the file-based per-transaction durability store (ADR 0002): folder created at
/// tray open, <c>journal.json</c> rewritten crash-safely on every transition,
/// <c>transactionLog.txt</c> appended per step, and the legacy-shaped
/// <c>transactionDetails.json</c> written at settlement.
/// </summary>
public interface ISessionStore
{
    /// <summary>
    /// Creates the per-transaction folder (<c>{root}/{dd-MM-yyyy}/{HH-mm-ss}/</c>) and
    /// attaches it to the session. Called at tray open — before any hardware moves.
    /// </summary>
    /// <param name="session">The session to create the folder for.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the folder and initial journal exist.</returns>
    Task CreateTransactionFolderAsync(TransactionSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rewrites <c>journal.json</c> from the session's current state using
    /// write-temp-then-replace so a crash never leaves a torn record. No-op until the
    /// transaction folder exists.
    /// </summary>
    /// <param name="session">The session to persist.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the journal is durable.</returns>
    Task PersistAsync(TransactionSession session, CancellationToken cancellationToken = default);

    /// <summary>Appends one step line to <c>transactionLog.txt</c>. No-op until the folder exists.</summary>
    /// <param name="session">The session whose log to append.</param>
    /// <param name="message">The step description. Never PII, never secrets.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the line is written.</returns>
    Task AppendLogAsync(TransactionSession session, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the legacy-shaped <c>transactionDetails.json</c> (field names and casing 1:1
    /// with the legacy parity contract) at settlement.
    /// </summary>
    /// <param name="session">The settled session.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the record is durable.</returns>
    Task WriteTransactionDetailsAsync(TransactionSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a binary artifact (e.g. <c>signImage.png</c>, <c>customerImage.png</c> —
    /// legacy file names preserved) into the transaction folder. No-op until the folder exists.
    /// </summary>
    /// <param name="session">The session whose folder receives the artifact.</param>
    /// <param name="fileName">The artifact file name (no path separators).</param>
    /// <param name="content">The artifact bytes. Never logged.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the artifact is written.</returns>
    Task WriteArtifactAsync(
        TransactionSession session,
        string fileName,
        byte[] content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans today's and yesterday's transaction folders for journals whose state is not
    /// terminal — sessions interrupted by a crash/restart (including just before
    /// midnight), for resume-or-safe-abort.
    /// </summary>
    /// <param name="cancellationToken">Cancels the scan.</param>
    /// <returns>The interrupted sessions found, possibly empty.</returns>
    Task<IReadOnlyList<RecoveredSession>> RecoverCurrentDayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Safe-aborts a recovered session: marks its journal terminal with a fault abort
    /// reason and appends the audit line to the transaction log.
    /// </summary>
    /// <param name="recovered">The recovered session to close out.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the journal is durable.</returns>
    Task SafeAbortAsync(RecoveredSession recovered, CancellationToken cancellationToken = default);
}
