using System.Net.Http;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Sessions;
using GoldKiosk.Kiosk.UI.Logging;
using GoldKiosk.Kiosk.UI.Resources;
using Microsoft.Extensions.Logging;

namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// Drives the UI flow around the server-owned session state machine: state → route
/// mapping, the local idle timer (touch-reset from the root pointer handler, with the
/// server's <c>idle_warning</c> folded in), the timeout/error overlays, and the one-shot
/// settle command once a payout is confirmed.
/// </summary>
public sealed class FlowController : IDisposable
{
    private static readonly HashSet<string> _idleWatchedStates =
    [
        SessionStates.Welcome,
        SessionStates.PlacingItem,
        SessionStates.Offer,
        SessionStates.Identity,
        SessionStates.Contact,
        SessionStates.Payout,
    ];

    private readonly KioskApiClient _api;
    private readonly SessionStore _store;
    private readonly KioskUiOptions _options;
    private readonly ILocalizedStrings _strings;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<FlowController> _logger;
    private readonly ITimer _ticker;
    private readonly object _gate = new();

    private DateTimeOffset _lastActivity;
    private bool _abortInFlight;
    private string? _settleRequestedForSession;

    /// <summary>Initializes the controller and starts the one-second idle ticker.</summary>
    /// <param name="api">The kiosk API client.</param>
    /// <param name="store">The session store.</param>
    /// <param name="hub">The hub client (for <c>idle_warning</c> and failure events).</param>
    /// <param name="options">The kiosk UI options.</param>
    /// <param name="strings">The string catalogue.</param>
    /// <param name="timeProvider">The time provider.</param>
    /// <param name="logger">The logger.</param>
    public FlowController(
        KioskApiClient api,
        SessionStore store,
        KioskHubClient hub,
        KioskUiOptions options,
        ILocalizedStrings strings,
        TimeProvider timeProvider,
        ILogger<FlowController> logger)
    {
        ArgumentNullException.ThrowIfNull(hub);
        _api = api;
        _store = store;
        _options = options;
        _strings = strings;
        _timeProvider = timeProvider;
        _logger = logger;
        _lastActivity = timeProvider.GetUtcNow();

        _store.Changed += OnStoreChanged;
        hub.IdleWarning += payload => ShowIdleWarning(payload.SecondsRemaining);
        hub.IdentityProgress += payload =>
        {
            if (payload.Status == "failed" && payload.Failure is { } failure)
            {
                ShowError(ErrorCatalog.ForIdentityFailure(failure, _strings));
            }
        };
        hub.SessionStateChanged += payload =>
        {
            if (payload.Rejection is { } rejection)
            {
                ShowError(ErrorCatalog.ForRejection(rejection, _strings));
            }
        };
        hub.SessionAborted += _ => ClearOverlays();
        hub.SessionCompleted += _ => ClearOverlays();

        _ticker = timeProvider.CreateTimer(_ => OnTick(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
    }

    /// <summary>Raised when overlay state changes. Components marshal via <c>InvokeAsync</c>.</summary>
    public event Action? Changed;

    /// <summary>Gets the active error overlay content, when one is showing.</summary>
    public ErrorPresentation? Error { get; private set; }

    /// <summary>Gets the timeout-overlay countdown in seconds, when it is showing.</summary>
    public int? TimeoutSecondsRemaining { get; private set; }

    /// <summary>Maps a session state to its screen route.</summary>
    /// <param name="state">The session state (see <see cref="SessionStates"/>).</param>
    /// <returns>The route for the screen owning that state.</returns>
    public static string RouteForState(string state) => state switch
    {
        SessionStates.Welcome => "/welcome",
        SessionStates.PlacingItem => "/place-item",
        SessionStates.Analyzing => "/analysis",
        SessionStates.Offer => "/offer",
        SessionStates.Identity => "/identity",
        SessionStates.Contact => "/contact",
        SessionStates.Payout => "/payout",
        SessionStates.PayoutConfirmed or SessionStates.Settling or SessionStates.ReturningItem => "/processing",
        SessionStates.Done => "/done",
        _ => "/",
    };

    /// <summary>
    /// Records customer activity (root-level pointer handler). Ignored while the timeout
    /// overlay is showing — the overlay owns interaction until answered.
    /// </summary>
    public void NotifyUserActivity()
    {
        lock (_gate)
        {
            if (TimeoutSecondsRemaining is not null)
            {
                return;
            }

            _lastActivity = _timeProvider.GetUtcNow();
        }
    }

    /// <summary>Resumes the session from the timeout overlay ("Yes — I'm here").</summary>
    public void ResumeSession()
    {
        lock (_gate)
        {
            _lastActivity = _timeProvider.GetUtcNow();
            TimeoutSecondsRemaining = null;
        }

        RaiseChanged();
    }

    /// <summary>
    /// Aborts the session, returning the item when the machine holds one.
    /// </summary>
    /// <param name="reason">The abort reason: <c>timeout</c>, <c>user_cancel</c>, <c>operator</c> or <c>fault</c>.</param>
    /// <returns>A task that completes when the abort command was sent.</returns>
    public async Task AbortAsync(string reason)
    {
        string? sessionId = _store.SessionId;
        if (sessionId is null)
        {
            return;
        }

        lock (_gate)
        {
            if (_abortInFlight)
            {
                return;
            }

            _abortInFlight = true;
        }

        try
        {
            bool returnItem = ItemIsHeld(_store.State);
            SessionActionResponse response = await _api.AbortSessionAsync(
                sessionId, new AbortSessionRequest(reason, returnItem));
            _store.ApplyAction(response);
            ClearOverlays();
        }
        catch (Exception ex) when (ex is KioskApiException or HttpRequestException or TaskCanceledException)
        {
            _logger.AbortFailed(ex, reason, sessionId);
            ShowApiError(ex);
        }
        finally
        {
            lock (_gate)
            {
                _abortInFlight = false;
            }
        }
    }

    /// <summary>
    /// Resolves an API/transport failure to a localized error overlay. Call from every
    /// screen-level catch.
    /// </summary>
    /// <param name="exception">The failure.</param>
    public void ShowApiError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        _logger.ApiCallFailed(exception);
        ErrorPresentation presentation = exception switch
        {
            KioskApiException api => ErrorCatalog.ForProblem(api.Problem, _strings),
            HttpRequestException or TaskCanceledException => ErrorCatalog.ForConnectivity(_strings),
            _ => ErrorCatalog.ForProblem(null, _strings),
        };
        ShowError(presentation);
    }

    /// <summary>Shows an error overlay.</summary>
    /// <param name="presentation">The localized error content.</param>
    public void ShowError(ErrorPresentation presentation)
    {
        lock (_gate)
        {
            Error = presentation;
        }

        RaiseChanged();
    }

    /// <summary>Dismisses the error overlay.</summary>
    public void DismissError()
    {
        lock (_gate)
        {
            Error = null;
        }

        RaiseChanged();
    }

    /// <summary>Executes a recovery button from the error overlay.</summary>
    /// <param name="action">The chosen recovery action.</param>
    /// <returns>A task that completes when the action was executed.</returns>
    public async Task ExecuteRecoveryAsync(RecoveryAction action)
    {
        DismissError();
        switch (action)
        {
            case RecoveryAction.ReturnItem:
                await AbortAsync("user_cancel");
                break;
            case RecoveryAction.EndSession:
                await AbortAsync("user_cancel");
                break;
            case RecoveryAction.BackToStart:
                _store.Reset();
                break;
            case RecoveryAction.Dismiss:
            default:
                break;
        }
    }

    /// <summary>Ends the completed session and returns to the attract loop (done-screen timer).</summary>
    public void CompleteToAttract()
    {
        ClearOverlays();
        _store.Reset();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _store.Changed -= OnStoreChanged;
        _ticker.Dispose();
    }

    private static bool ItemIsHeld(string state) => state
        is SessionStates.Analyzing
        or SessionStates.Offer
        or SessionStates.Identity
        or SessionStates.Contact
        or SessionStates.Payout
        or SessionStates.PayoutConfirmed;

    private void OnStoreChanged()
    {
        // One-shot settle once the payout is confirmed; the settle endpoint is idempotent.
        if (_store.State == SessionStates.PayoutConfirmed && _store.SessionId is { } sessionId)
        {
            lock (_gate)
            {
                if (_settleRequestedForSession == sessionId)
                {
                    return;
                }

                _settleRequestedForSession = sessionId;
            }

            _ = SettleSafelyAsync(sessionId);
        }
    }

    private async Task SettleSafelyAsync(string sessionId)
    {
        try
        {
            SessionActionResponse response = await _api.SettleAsync(sessionId);
            _store.ApplyAction(response);
        }
        catch (Exception ex) when (ex is KioskApiException or HttpRequestException or TaskCanceledException)
        {
            _logger.SettleFailed(ex, sessionId);
            lock (_gate)
            {
                // Allow a retry on the next state notification.
                _settleRequestedForSession = null;
            }

            ShowApiError(ex);
        }
    }

    private void ShowIdleWarning(int secondsRemaining)
    {
        lock (_gate)
        {
            TimeoutSecondsRemaining = secondsRemaining;
            // Align the local clock with the server's warning.
            int idleTimeout = EffectiveIdleTimeout();
            _lastActivity = _timeProvider.GetUtcNow() - TimeSpan.FromSeconds(idleTimeout - secondsRemaining);
        }

        RaiseChanged();
    }

    private int EffectiveIdleTimeout() =>
        _store.IdleTimeoutSeconds > 0 ? _store.IdleTimeoutSeconds : 90;

    private void OnTick()
    {
        bool changed = false;
        bool abort = false;
        lock (_gate)
        {
            if (!_idleWatchedStates.Contains(_store.State) || _store.SessionId is null)
            {
                if (TimeoutSecondsRemaining is not null)
                {
                    TimeoutSecondsRemaining = null;
                    changed = true;
                }

                _lastActivity = _timeProvider.GetUtcNow();
            }
            else
            {
                int idleTimeout = EffectiveIdleTimeout();
                int elapsed = (int)(_timeProvider.GetUtcNow() - _lastActivity).TotalSeconds;
                int remaining = idleTimeout - elapsed;
                if (remaining <= 0)
                {
                    TimeoutSecondsRemaining = null;
                    changed = true;
                    abort = true;
                }
                else if (remaining <= _options.IdleGraceSeconds)
                {
                    if (TimeoutSecondsRemaining != remaining)
                    {
                        TimeoutSecondsRemaining = remaining;
                        changed = true;
                    }
                }
                else if (TimeoutSecondsRemaining is not null)
                {
                    TimeoutSecondsRemaining = null;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            RaiseChanged();
        }

        if (abort)
        {
            _ = AbortAsync("timeout");
        }
    }

    private void ClearOverlays()
    {
        lock (_gate)
        {
            Error = null;
            TimeoutSecondsRemaining = null;
        }

        RaiseChanged();
    }

    private void RaiseChanged() => Changed?.Invoke();
}
