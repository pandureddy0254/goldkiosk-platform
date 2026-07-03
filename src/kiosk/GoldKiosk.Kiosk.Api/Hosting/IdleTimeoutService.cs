using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Events;
using GoldKiosk.Kiosk.Api.Logging;
using GoldKiosk.Kiosk.Api.Orchestration;
using GoldKiosk.Kiosk.Core.Options;
using GoldKiosk.Kiosk.Core.Orchestration;
using GoldKiosk.Kiosk.Core.Sessions;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Kiosk.Api.Hosting;

/// <summary>
/// The idle-timeout watchdog: every REST call touches the session; when inactivity
/// approaches the configured timeout an <c>idle_warning</c> event drives the countdown
/// ring, and at zero the session auto-aborts with <c>timeout</c>, returning a held item.
/// Only customer-driven states are watched — machine-driven states (analyzing, settling,
/// returning) never idle-abort.
/// </summary>
public sealed class IdleTimeoutService : BackgroundService
{
    private const int WarningWindowSeconds = 15;

    // Mirrors the UI's idle-watched states (FlowController): states where the customer
    // must act. Excludes settling / returning_item / payout_confirmed (money or item in
    // motion — aborting there risks double payout) and analyzing (machine-driven).
    private static readonly HashSet<string> _idleWatchedStates = new(StringComparer.Ordinal)
    {
        SessionStates.Welcome,
        SessionStates.PlacingItem,
        SessionStates.Offer,
        SessionStates.Identity,
        SessionStates.Contact,
        SessionStates.Payout,
    };

    private readonly SessionRegistry _sessions;
    private readonly TransactionOrchestrator _orchestrator;
    private readonly IKioskEventPublisher _events;
    private readonly KioskOptions _kiosk;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<IdleTimeoutService> _logger;

    /// <summary>Initializes the watchdog.</summary>
    /// <param name="sessions">The in-memory session registry.</param>
    /// <param name="orchestrator">The orchestrator used to auto-abort.</param>
    /// <param name="events">The event publisher for warnings.</param>
    /// <param name="kioskOptions">The kiosk options carrying the idle timeout.</param>
    /// <param name="timeProvider">The time source.</param>
    /// <param name="logger">The host logger.</param>
    public IdleTimeoutService(
        SessionRegistry sessions,
        TransactionOrchestrator orchestrator,
        IKioskEventPublisher events,
        IOptions<KioskOptions> kioskOptions,
        TimeProvider timeProvider,
        ILogger<IdleTimeoutService> logger)
    {
        ArgumentNullException.ThrowIfNull(kioskOptions);

        _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _kiosk = kioskOptions.Value;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), _timeProvider);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckSessionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Deliberate catch-all: one failed sweep must never stop the watchdog —
                // an unwatched kiosk would hold sessions (and items) forever.
                _logger.IdleWatchdogSweepFailed(ex);
            }
        }
    }

    private async Task CheckSessionsAsync(CancellationToken stoppingToken)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        foreach (TransactionSession session in _sessions.ActiveSessions)
        {
            if (!_idleWatchedStates.Contains(session.State))
            {
                continue;
            }

            double idleSeconds = (now - session.LastActivityAt).TotalSeconds;
            double remaining = _kiosk.IdleTimeoutSeconds - idleSeconds;
            if (remaining <= 0)
            {
                _logger.IdleTimeoutAborting(session.Id, (int)idleSeconds);
                await _orchestrator.AbortAsync(session, "timeout", returnItem: true, stoppingToken);
            }
            else if (remaining <= WarningWindowSeconds)
            {
                await _events.PublishIdleWarningAsync(
                    new IdleWarningEvent(
                        session.Id,
                        session.NextSequence(),
                        (int)Math.Ceiling(remaining)),
                    stoppingToken);
            }
        }
    }
}
