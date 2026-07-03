using GoldKiosk.Contracts.V1.Events;
using GoldKiosk.Contracts.V1.Hub;
using GoldKiosk.Kiosk.UI.Logging;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// The SignalR connection to the Kiosk API hub (<c>/hubs/kiosk</c>, server → client only).
/// Dispatches the ten <see cref="IKioskHubClient"/> events onto plain C# events; consumers
/// marshal to the UI thread themselves (components via <c>InvokeAsync</c>). REST remains
/// the source of truth — on reconnect the session store resyncs from the snapshot.
/// </summary>
public sealed class KioskHubClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger<KioskHubClient> _logger;

    /// <summary>Initializes the hub client (does not connect yet).</summary>
    /// <param name="options">The kiosk UI options carrying the API base URL.</param>
    /// <param name="logger">The logger.</param>
    public KioskHubClient(KioskUiOptions options, ILogger<KioskHubClient> logger)
    {
        _logger = logger;

        string hubUrl = options.ApiBaseUrl.TrimEnd('/') + "/hubs/kiosk";
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .AddJsonProtocol(json => json.PayloadSerializerOptions = KioskJson.Options)
            .Build();

        RegisterHandlers();

        _connection.Reconnected += _ =>
        {
            _logger.HubReconnected();
            Reconnected?.Invoke();
            return Task.CompletedTask;
        };
        _connection.Closed += error =>
        {
            _logger.HubClosed(error);
            return Task.CompletedTask;
        };
    }

    /// <summary>Raised when the <c>session_state_changed</c> event arrives.</summary>
    public event Action<SessionStateChangedEvent>? SessionStateChanged;

    /// <summary>Raised when the <c>tray_state_changed</c> event arrives.</summary>
    public event Action<TrayStateChangedEvent>? TrayStateChanged;

    /// <summary>Raised when the <c>analysis_progress</c> event arrives.</summary>
    public event Action<AnalysisProgressEvent>? AnalysisProgress;

    /// <summary>Raised when the <c>identity_progress</c> event arrives.</summary>
    public event Action<IdentityProgressEvent>? IdentityProgress;

    /// <summary>Raised when the <c>settlement_progress</c> event arrives.</summary>
    public event Action<SettlementProgressEvent>? SettlementProgress;

    /// <summary>Raised when the <c>agent_status</c> event arrives.</summary>
    public event Action<AgentStatusEvent>? AgentStatus;

    /// <summary>Raised when the <c>device_health_changed</c> event arrives.</summary>
    public event Action<DeviceHealthChangedEvent>? DeviceHealthChanged;

    /// <summary>Raised when the <c>session_completed</c> event arrives.</summary>
    public event Action<SessionCompletedEvent>? SessionCompleted;

    /// <summary>Raised when the <c>session_aborted</c> event arrives.</summary>
    public event Action<SessionAbortedEvent>? SessionAborted;

    /// <summary>Raised when the <c>idle_warning</c> event arrives.</summary>
    public event Action<IdleWarningEvent>? IdleWarning;

    /// <summary>Raised after the connection automatically reconnected (snapshot resync trigger).</summary>
    public event Action? Reconnected;

    /// <summary>Gets a value indicating whether the hub connection is currently up.</summary>
    public bool IsConnected => _connection.State == HubConnectionState.Connected;

    /// <summary>
    /// Connects to the hub, retrying with a fixed backoff until it succeeds or the token
    /// is cancelled. A kiosk without its local API is not operable, so this never gives up.
    /// </summary>
    /// <param name="cancellationToken">A token to stop retrying.</param>
    /// <returns>A task that completes once connected.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _connection.StartAsync(cancellationToken);
                _logger.HubConnected();
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.HubConnectRetrying(ex);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }

    private void RegisterHandlers()
    {
        _connection.On<SessionStateChangedEvent>(
            nameof(IKioskHubClient.SessionStateChanged), payload => SessionStateChanged?.Invoke(payload));
        _connection.On<TrayStateChangedEvent>(
            nameof(IKioskHubClient.TrayStateChanged), payload => TrayStateChanged?.Invoke(payload));
        _connection.On<AnalysisProgressEvent>(
            nameof(IKioskHubClient.AnalysisProgress), payload => AnalysisProgress?.Invoke(payload));
        _connection.On<IdentityProgressEvent>(
            nameof(IKioskHubClient.IdentityProgress), payload => IdentityProgress?.Invoke(payload));
        _connection.On<SettlementProgressEvent>(
            nameof(IKioskHubClient.SettlementProgress), payload => SettlementProgress?.Invoke(payload));
        _connection.On<AgentStatusEvent>(
            nameof(IKioskHubClient.AgentStatus), payload => AgentStatus?.Invoke(payload));
        _connection.On<DeviceHealthChangedEvent>(
            nameof(IKioskHubClient.DeviceHealthChanged), payload => DeviceHealthChanged?.Invoke(payload));
        _connection.On<SessionCompletedEvent>(
            nameof(IKioskHubClient.SessionCompleted), payload => SessionCompleted?.Invoke(payload));
        _connection.On<SessionAbortedEvent>(
            nameof(IKioskHubClient.SessionAborted), payload => SessionAborted?.Invoke(payload));
        _connection.On<IdleWarningEvent>(
            nameof(IKioskHubClient.IdleWarning), payload => IdleWarning?.Invoke(payload));
    }
}
