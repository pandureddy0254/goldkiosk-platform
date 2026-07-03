using System.Net.Http;
using System.Text.Json;
using GoldKiosk.Contracts.V1.Agent;
using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Events;
using GoldKiosk.Contracts.V1.Identity;
using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Contracts.V1.Payout;
using GoldKiosk.Contracts.V1.Sessions;
using GoldKiosk.Contracts.V1.Settlement;
using GoldKiosk.Contracts.V1.Tray;
using GoldKiosk.Kiosk.UI.Logging;
using Microsoft.Extensions.Logging;

namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// The single client-side session state: last REST snapshot merged with SignalR events.
/// Events carry a per-session monotonic sequence — anything at or below the last seen
/// sequence is ignored. On hub reconnect the store resyncs from <c>GET /sessions/{id}</c>
/// (the crash/resume path). Also accumulates batched client telemetry events, which only
/// ever ship inside the clubbed tray payloads.
/// </summary>
public sealed class SessionStore
{
    private static readonly string[] _defaultIdentityOrder = ["id_scan", "face_match", "signature"];
    private static readonly string[] _fingerprintIdentityOrder = ["id_scan", "face_match", "fingerprint", "signature"];

    private readonly object _gate = new();
    private readonly List<ClientEventDto> _clientEvents = [];
    private readonly KioskApiClient _api;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SessionStore> _logger;

    /// <summary>Initializes the store and subscribes to the hub event stream.</summary>
    /// <param name="hub">The kiosk hub client.</param>
    /// <param name="api">The kiosk API client (snapshot resync).</param>
    /// <param name="timeProvider">The time provider for client-event timestamps.</param>
    /// <param name="logger">The logger.</param>
    public SessionStore(
        KioskHubClient hub, KioskApiClient api, TimeProvider timeProvider, ILogger<SessionStore> logger)
    {
        _api = api;
        _timeProvider = timeProvider;
        _logger = logger;

        hub.SessionStateChanged += OnSessionStateChanged;
        hub.TrayStateChanged += OnTrayStateChanged;
        hub.AnalysisProgress += OnAnalysisProgress;
        hub.IdentityProgress += OnIdentityProgress;
        hub.SettlementProgress += OnSettlementProgress;
        hub.AgentStatus += OnAgentStatus;
        hub.DeviceHealthChanged += OnDeviceHealthChanged;
        hub.SessionCompleted += OnSessionCompleted;
        hub.SessionAborted += OnSessionAborted;
        hub.Reconnected += () => _ = ResyncAsync();
    }

    /// <summary>Raised whenever any state changes. Components marshal via <c>InvokeAsync</c>.</summary>
    public event Action? Changed;

    /// <summary>Gets the current session identifier, or <see langword="null"/> at the attract loop.</summary>
    public string? SessionId { get; private set; }

    /// <summary>Gets the current session state (see <see cref="SessionStates"/>).</summary>
    public string State { get; private set; } = SessionStates.Attract;

    /// <summary>Gets the last event sequence applied.</summary>
    public long LastSequence { get; private set; }

    /// <summary>Gets the feature flags for this session.</summary>
    public FeaturesDto? Features { get; private set; }

    /// <summary>Gets the terms version the customer must accept.</summary>
    public string TermsVersion { get; private set; } = string.Empty;

    /// <summary>Gets the idle timeout before automatic abort, in seconds.</summary>
    public int IdleTimeoutSeconds { get; private set; }

    /// <summary>Gets the BCP 47 locale in effect for this session.</summary>
    public string Locale { get; private set; } = "en-US";

    /// <summary>Gets the chosen service type (<c>sell</c> or <c>pawn</c>), once selected.</summary>
    public string? ServiceType { get; private set; }

    /// <summary>Gets the tray movement status (<c>opening</c>, <c>open</c>, <c>closing</c>, <c>closed</c>).</summary>
    public string TrayStatus { get; private set; } = "closed";

    /// <summary>Gets the current offer, once made.</summary>
    public OfferDto? Offer { get; private set; }

    /// <summary>Gets the identity checklist steps, once the identity sequence started.</summary>
    public IReadOnlyList<IdentityStepDto> IdentitySteps { get; private set; } = [];

    /// <summary>Gets the identity step currently in progress, when one is.</summary>
    public string? CurrentIdentityStep { get; private set; }

    /// <summary>Gets a value indicating whether the identity sequence has been started.</summary>
    public bool IdentityStarted { get; private set; }

    /// <summary>Gets the confirmed payout status, once a method was accepted.</summary>
    public PayoutStatusDto? Payout { get; private set; }

    /// <summary>Gets the terminal receipt, once the session completed.</summary>
    public ReceiptDto? Receipt { get; private set; }

    /// <summary>Gets the rejection details, when the item or customer was rejected.</summary>
    public RejectionDto? Rejection { get; private set; }

    /// <summary>Gets the abort reason, when the session was aborted.</summary>
    public string? AbortReason { get; private set; }

    /// <summary>Gets the latest analysis stage (<c>item_detected</c>, <c>authenticating</c>, <c>pricing</c>).</summary>
    public string? AnalysisStage { get; private set; }

    /// <summary>Gets the overall analysis progress percentage, 0–100.</summary>
    public int AnalysisProgress { get; private set; }

    /// <summary>Gets the live-agent review status, when an escalation is in flight.</summary>
    public AgentStatusDto? AgentStatus { get; private set; }

    /// <summary>Gets the aggregate hardware health (<c>healthy</c>, <c>degraded</c>, <c>faulted</c>).</summary>
    public string DeviceHealth { get; private set; } = "healthy";

    /// <summary>Gets the latest settlement stage (<c>bagging</c>, <c>dispensing</c>, <c>transferring</c>).</summary>
    public string? SettlementStage { get; private set; }

    /// <summary>Gets the customer-facing settlement display text.</summary>
    public string? SettlementDisplay { get; private set; }

    /// <summary>Gets the bill mix being dispensed (denomination → count), for cash payouts.</summary>
    public IReadOnlyDictionary<string, int>? SettlementBills { get; private set; }

    /// <summary>Gets a value indicating whether the session ran with one or more mocked devices.</summary>
    public bool IsTest { get; private set; }

    /// <summary>Gets a value indicating whether a customer session is currently active.</summary>
    public bool HasActiveSession =>
        SessionId is not null && State != SessionStates.Attract && State != SessionStates.Done;

    /// <summary>Applies the begin-session response and moves the UI into the session.</summary>
    /// <param name="response">The begin-session response.</param>
    /// <param name="locale">The locale the session was begun with.</param>
    public void BeginSession(BeginSessionResponse response, string locale)
    {
        ArgumentNullException.ThrowIfNull(response);
        lock (_gate)
        {
            ClearSessionState();
            SessionId = response.SessionId;
            State = response.State;
            LastSequence = response.Sequence;
            Features = response.Features;
            IdleTimeoutSeconds = response.IdleTimeoutSeconds;
            TermsVersion = response.TermsVersion;
            Locale = locale;
        }

        RaiseChanged();
    }

    /// <summary>Records the chosen service type (accumulated for the clubbed tray-open payload).</summary>
    /// <param name="serviceType">The service type, <c>sell</c> or <c>pawn</c>.</param>
    public void SelectService(string serviceType)
    {
        lock (_gate)
        {
            ServiceType = serviceType;
        }

        RaiseChanged();
    }

    /// <summary>
    /// Queues a UI telemetry event. Events are never posted individually — they ship in
    /// the next clubbed tray payload via <see cref="DrainClientEvents"/>.
    /// </summary>
    /// <param name="name">The event name, e.g. <c>attract_engaged</c>.</param>
    /// <param name="data">An optional payload object, serialized snake_case.</param>
    public void QueueClientEvent(string name, object? data = null)
    {
        JsonElement? element = data is null
            ? null
            : JsonSerializer.SerializeToElement(data, data.GetType(), KioskJson.Options);
        lock (_gate)
        {
            _clientEvents.Add(new ClientEventDto(_timeProvider.GetLocalNow(), name, element));
        }
    }

    /// <summary>Drains all queued client telemetry events for a clubbed payload.</summary>
    /// <returns>The queued events, oldest first.</returns>
    public IReadOnlyList<ClientEventDto> DrainClientEvents()
    {
        lock (_gate)
        {
            ClientEventDto[] drained = [.. _clientEvents];
            _clientEvents.Clear();
            return drained;
        }
    }

    /// <summary>
    /// Applies a tray command response (REST is the source of truth). Applied only when
    /// the response sequence is newer than the last seen — a stale response arriving
    /// after fresher SignalR events never rolls state back (aligned with
    /// <see cref="ApplyAction"/>).
    /// </summary>
    /// <param name="response">The tray state response.</param>
    public void ApplyTray(TrayStateResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        bool applied = false;
        lock (_gate)
        {
            if (response.Sequence > LastSequence)
            {
                LastSequence = response.Sequence;
                SetStateLocked(response.State);
                TrayStatus = response.Tray.Status;
                applied = true;
            }
        }

        if (applied)
        {
            RaiseChanged();
        }
    }

    /// <summary>Applies a session command response (REST is the source of truth).</summary>
    /// <param name="response">The command response.</param>
    public void ApplyAction(SessionActionResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        lock (_gate)
        {
            if (response.Sequence is { } sequence && sequence > LastSequence)
            {
                LastSequence = sequence;
            }

            if (response.State is { Length: > 0 } state)
            {
                SetStateLocked(state);
            }

            if (response.Identity is { } identity)
            {
                ApplyIdentityProgressLocked(identity);
            }

            if (response.Payout is { } payout)
            {
                Payout = payout;
            }
        }

        RaiseChanged();
    }

    /// <summary>Marks the identity sequence as started (guards the one-shot start call).</summary>
    public void MarkIdentityStarted()
    {
        lock (_gate)
        {
            IdentityStarted = true;
        }
    }

    /// <summary>
    /// Resyncs from the full session snapshot — the crash/reload and hub-reconnect path.
    /// The snapshot is authoritative and overwrites local state.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>A task that completes when the snapshot was applied.</returns>
    public async Task ResyncAsync(CancellationToken cancellationToken = default)
    {
        string? sessionId = SessionId;
        if (sessionId is null)
        {
            return;
        }

        try
        {
            SessionSnapshotResponse snapshot = await _api.GetSessionAsync(sessionId, cancellationToken);
            lock (_gate)
            {
                if (SessionId != snapshot.SessionId)
                {
                    return;
                }

                State = snapshot.State;
                LastSequence = snapshot.Sequence;
                Offer = snapshot.Offer ?? Offer;
                if (snapshot.Identity is { } identity)
                {
                    ApplyIdentityProgressLocked(identity);
                }

                Payout = snapshot.Payout ?? Payout;
                IsTest = snapshot.IsTest;
            }

            RaiseChanged();
        }
        catch (Exception ex) when (ex is KioskApiException or HttpRequestException or TaskCanceledException)
        {
            _logger.ResyncFailed(ex, sessionId);
        }
    }

    /// <summary>Clears everything and returns the UI to the attract loop.</summary>
    public void Reset()
    {
        lock (_gate)
        {
            ClearSessionState();
            _clientEvents.Clear();
            State = SessionStates.Attract;
        }

        RaiseChanged();
    }

    private void ClearSessionState()
    {
        // Note: queued client events survive — attract-loop events queued just before
        // BeginSession belong to the new session and ship with the first tray payload.
        SessionId = null;
        State = SessionStates.Attract;
        LastSequence = 0;
        Features = null;
        IdleTimeoutSeconds = 0;
        TermsVersion = string.Empty;
        ServiceType = null;
        TrayStatus = "closed";
        Offer = null;
        IdentitySteps = [];
        CurrentIdentityStep = null;
        IdentityStarted = false;
        Payout = null;
        Receipt = null;
        Rejection = null;
        AbortReason = null;
        AnalysisStage = null;
        AnalysisProgress = 0;
        AgentStatus = null;
        SettlementStage = null;
        SettlementDisplay = null;
        SettlementBills = null;
        IsTest = false;
    }

    private void SetStateLocked(string state)
    {
        State = state;
        if (state == SessionStates.Identity && IdentitySteps.Count == 0)
        {
            InitializeIdentityStepsLocked();
        }
    }

    private void InitializeIdentityStepsLocked()
    {
        string[] order = Features?.FingerprintRequired == true
            ? _fingerprintIdentityOrder
            : _defaultIdentityOrder;
        IdentitySteps = [.. order.Select(step => new IdentityStepDto(step, "pending", null))];
    }

    private void ApplyIdentityProgressLocked(IdentityProgressDto progress)
    {
        if (IdentitySteps.Count == 0)
        {
            InitializeIdentityStepsLocked();
        }

        // The identity/start REST response carries only current_step (steps list is null);
        // full step lists arrive via identity_progress hub events. Guard both shapes.
        if (progress.Steps is { Count: > 0 })
        {
            IdentitySteps = progress.Steps;
        }

        CurrentIdentityStep = progress.CurrentStep ?? CurrentIdentityStep;
        IdentityStarted = true;
    }

    private bool Accept(string sessionId, long sequence)
    {
        return SessionId is not null
            && string.Equals(sessionId, SessionId, StringComparison.Ordinal)
            && sequence > LastSequence;
    }

    private void Apply(string sessionId, long sequence, Action mutate)
    {
        bool applied = false;
        lock (_gate)
        {
            if (Accept(sessionId, sequence))
            {
                LastSequence = sequence;
                mutate();
                applied = true;
            }
        }

        if (applied)
        {
            RaiseChanged();
        }
    }

    private void OnSessionStateChanged(SessionStateChangedEvent payload) =>
        Apply(payload.SessionId, payload.Sequence, () =>
        {
            SetStateLocked(payload.State);
            Offer = payload.Offer ?? Offer;
            Rejection = payload.Rejection ?? Rejection;
        });

    private void OnTrayStateChanged(TrayStateChangedEvent payload) =>
        Apply(payload.SessionId, payload.Sequence, () => TrayStatus = payload.Tray.Status);

    private void OnAnalysisProgress(AnalysisProgressEvent payload) =>
        Apply(payload.SessionId, payload.Sequence, () =>
        {
            AnalysisStage = payload.Stage;
            AnalysisProgress = payload.Progress;
        });

    private void OnIdentityProgress(IdentityProgressEvent payload) =>
        Apply(payload.SessionId, payload.Sequence, () =>
        {
            if (IdentitySteps.Count == 0)
            {
                InitializeIdentityStepsLocked();
            }

            IdentitySteps = [.. IdentitySteps.Select(step =>
                step.Step == payload.Step
                    ? new IdentityStepDto(step.Step, payload.Status, payload.Failure)
                    : step)];
            CurrentIdentityStep = payload.Status == "in_progress" ? payload.Step : CurrentIdentityStep;
            if (payload.Status == "completed" && CurrentIdentityStep == payload.Step)
            {
                CurrentIdentityStep = null;
            }
        });

    private void OnSettlementProgress(SettlementProgressEvent payload) =>
        Apply(payload.SessionId, payload.Sequence, () =>
        {
            SettlementStage = payload.Stage;
            SettlementDisplay = payload.Display;
            SettlementBills = payload.Bills ?? SettlementBills;
        });

    private void OnAgentStatus(AgentStatusEvent payload) =>
        Apply(payload.SessionId, payload.Sequence, () =>
            AgentStatus = new AgentStatusDto(payload.Status, payload.AgentRef));

    private void OnDeviceHealthChanged(DeviceHealthChangedEvent payload)
    {
        lock (_gate)
        {
            // Device health is kiosk-wide: apply regardless of session gating.
            DeviceHealth = payload.Overall;
        }

        RaiseChanged();
    }

    private void OnSessionCompleted(SessionCompletedEvent payload) =>
        Apply(payload.SessionId, payload.Sequence, () =>
        {
            SetStateLocked(payload.State);
            Receipt = payload.Receipt;
            IsTest = payload.IsTest;
        });

    private void OnSessionAborted(SessionAbortedEvent payload) =>
        Apply(payload.SessionId, payload.Sequence, () =>
        {
            AbortReason = payload.Reason;
            SetStateLocked(SessionStates.Done);
        });

    private void RaiseChanged() => Changed?.Invoke();
}
