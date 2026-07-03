using GoldKiosk.Kiosk.Api.Logging;
using GoldKiosk.Kiosk.Core.Persistence;

namespace GoldKiosk.Kiosk.Api.Hosting;

/// <summary>
/// Startup recovery per ADR 0002: scans today's transaction folders for journals stuck in
/// a non-terminal state (crash/restart mid-session) and safe-aborts each with an audit
/// line. Hardware state cannot be trusted across a restart, so resume is never attempted —
/// safe-abort is the deliberate policy.
/// </summary>
/// <param name="store">The file-based transaction store.</param>
/// <param name="logger">The host logger.</param>
public sealed class SessionRecoveryService(
    ISessionStore store,
    ILogger<SessionRecoveryService> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<RecoveredSession> recovered = await store.RecoverCurrentDayAsync(cancellationToken);
        if (recovered.Count == 0)
        {
            logger.RecoveryNoneFound();
            return;
        }

        foreach (RecoveredSession session in recovered)
        {
            logger.RecoveryInterruptedSession(session.Journal.SessionId, session.Journal.State);
            await store.SafeAbortAsync(session, cancellationToken);
        }

        logger.RecoverySafeAborted(recovered.Count);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
