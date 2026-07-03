using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Exceptions;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Protocol.Vanta;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real Olympus Vanta XRF driver: JSON commands over WebSocket
/// (<c>ws://192.168.7.2:7860</c> default) with a 1 Hz UDP <c>MsgFromPC</c> heartbeat on
/// :7862 keeping the controller session alive. Protocol per the legacy
/// <c>VantaGunClient</c>/<c>XrfClientWebSocket</c>: text handshake
/// (<c>Hi i am a controller</c> → <c>Server ready to receive commands.</c>), then per run
/// Login (301) → SetCurrentMethod (703, <c>preciousMetal-VLW</c>) → StartTest (601), results
/// arriving as commandId 403 notifications (206 result blobs, 209 errors, 103 battery
/// heartbeats). Final chemistry maps through <see cref="VantaChemistryMapper"/>.
/// </summary>
/// <remarks>
/// <b>Power-cycle recovery (legacy <c>PowerOnXRFGun</c> parity):</b> when the gun wedges
/// (connect fails or the probe reports a stale battery status), the session flow should call
/// <c>IPowerRelays.ToggleAnalyserPowerAsync</c> (relay pattern 7 → dwell → 3), wait ~15 s for
/// the gun to boot, then call <see cref="RealDeviceBase.ConnectAsync"/> again. The driver
/// itself never touches the relays — recovery is an orchestration decision in Kiosk.Api.
/// The <c>innovx</c> variant is served by <see cref="RealMetalAnalyserInnovX"/>, selected by
/// <c>Devices:Overrides:metal_analyser:Connection:Variant</c> at composition.
/// </remarks>
public sealed class RealMetalAnalyser : RealDeviceBase, IMetalAnalyser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConnectionOptions _connection;
    private readonly MetalAnalyserOptions _options;
    private readonly Lock _runLock = new();
    private readonly List<VantaResult> _finalResults = [];
    private ClientWebSocket? _webSocket;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _lifetime;
    private Task? _receiveLoop;
    private Task? _heartbeatLoop;
    private TaskCompletionSource<bool>? _readyTcs;
    private TaskCompletionSource<string?>? _runTcs;
    private DateTimeOffset? _lastBatteryStatus;
    private int _commandCounter;

    /// <summary>Initializes the driver with legacy-parity defaults (192.168.7.2:7860/7862).</summary>
    public RealMetalAnalyser()
        : this(null, null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="connection">Connection overrides; <see langword="null"/> uses <see cref="RealDeviceDefaults"/>.</param>
    /// <param name="options">Analyser tuning; <see langword="null"/> uses defaults.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealMetalAnalyser(
        ConnectionOptions? connection,
        MetalAnalyserOptions? options = null,
        TimeProvider? timeProvider = null)
        : base(DeviceKeys.MetalAnalyser, "XRF metal analyser (Olympus Vanta)", isCritical: true, timeProvider)
    {
        _connection = (connection ?? new ConnectionOptions())
            .MergedWith(RealDeviceDefaults.For(DeviceKeys.MetalAnalyser));
        _options = options ?? new MetalAnalyserOptions();
    }

    /// <inheritdoc />
    /// <remarks>Raised from the WebSocket receive loop; subscribers must not block.</remarks>
    public event EventHandler<AnalysisProgress>? ProgressChanged;

    /// <inheritdoc />
    public async Task<AnalysisRun> StartAnalysisAsync(CancellationToken cancellationToken = default)
    {
        EnsureOperable();
        SetHealth(DeviceState.Busy, "Analysis running.");
        try
        {
            TaskCompletionSource<string?> runTcs;
            lock (_runLock)
            {
                _finalResults.Clear();
                runTcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
                _runTcs = runTcs;
            }

            RaiseProgress(0, "item_detected");
            await SendCommandAsync(
                    VantaCommandId.Login,
                    new { password = _options.Password, userId = _options.UserId },
                    cancellationToken)
                .ConfigureAwait(false);
            await PaceAsync(_options.CommandPacingMs, cancellationToken).ConfigureAwait(false);
            await SendCommandAsync(VantaCommandId.SetCurrentMethod, new { methodId = _options.MethodId }, cancellationToken)
                .ConfigureAwait(false);
            await PaceAsync(_options.MethodActivationDelayMs, cancellationToken).ConfigureAwait(false);
            await SendCommandAsync(VantaCommandId.StartTest, null, cancellationToken).ConfigureAwait(false);
            RaiseProgress(25, "authenticating");

            string? runError;
            try
            {
                runError = await runTcs.Task
                    .WaitAsync(TimeSpan.FromMilliseconds(_options.AnalysisTimeoutMs), TimeProvider, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                runError = "analysis_timeout";
            }

            AnalysisRun run;
            if (runError is not null)
            {
                run = new AnalysisRun(false, runError, null, null, false, new Dictionary<string, decimal>());
            }
            else
            {
                lock (_runLock)
                {
                    run = VantaChemistryMapper.Map([.. _finalResults]);
                }
            }

            RaiseProgress(100, "pricing");
            await TryLogoutAsync(cancellationToken).ConfigureAwait(false);
            SetHealth(DeviceState.Ready);
            return run;
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Ready);
            throw;
        }
        catch (Exception ex)
        {
            SetHealth(DeviceState.Faulted, $"analysis_failed:{ex.GetType().Name}");
            throw;
        }
        finally
        {
            lock (_runLock)
            {
                _runTcs = null;
            }
        }
    }

    /// <inheritdoc />
    protected override async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        string host = _connection.Host!;
        int port = _connection.TcpPort!.Value;

        var webSocket = new ClientWebSocket();
        var lifetime = new CancellationTokenSource();
        UdpClient? udpClient = null;
        try
        {
            using var connectTimeout = new CancellationTokenSource(
                TimeSpan.FromMilliseconds(_options.ConnectTimeoutMs), TimeProvider);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, connectTimeout.Token);
            await webSocket.ConnectAsync(new Uri($"ws://{host}:{port}"), linked.Token).ConfigureAwait(false);

            _readyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _receiveLoop = Task.Run(() => ReceiveLoopAsync(webSocket, lifetime.Token), CancellationToken.None);

            await SendTextAsync(webSocket, VantaProtocolStrings.Handshake, cancellationToken).ConfigureAwait(false);
            try
            {
                await _readyTcs.Task
                    .WaitAsync(TimeSpan.FromMilliseconds(_options.ConnectTimeoutMs), TimeProvider, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                throw new DeviceConnectFailedException("vanta_handshake_timeout");
            }

            udpClient = new UdpClient();
            udpClient.Connect(host, _options.UdpPort);
            _heartbeatLoop = Task.Run(() => HeartbeatLoopAsync(udpClient, lifetime.Token), CancellationToken.None);
        }
        catch
        {
            lifetime.Cancel();
            udpClient?.Dispose();
            webSocket.Dispose();
            lifetime.Dispose();
            throw;
        }

        _webSocket = webSocket;
        _udpClient = udpClient;
        _lifetime = lifetime;
    }

    /// <inheritdoc />
    protected override async Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        await TryLogoutAsync(cancellationToken).ConfigureAwait(false);
        ReleaseTransports();
    }

    /// <inheritdoc />
    protected override Task<DeviceProbeResult> ProbeCoreAsync(CancellationToken cancellationToken)
    {
        if (_webSocket is not { State: WebSocketState.Open })
        {
            return Task.FromResult(new DeviceProbeResult(false, 0, Health.Detail ?? "vanta_socket_closed"));
        }

        DateTimeOffset? lastBattery = _lastBatteryStatus;
        if (lastBattery is null)
        {
            return Task.FromResult(new DeviceProbeResult(true, 0, "Socket open; no battery status received yet."));
        }

        double ageSeconds = (TimeProvider.GetUtcNow() - lastBattery.Value).TotalSeconds;
        return Task.FromResult(ageSeconds <= _options.BatteryStatusMaxAgeSeconds
            ? new DeviceProbeResult(true, 0, $"Battery status age {ageSeconds:F0}s.")
            : new DeviceProbeResult(false, 0, $"vanta_battery_status_stale:{ageSeconds:F0}s"));
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseTransports();
        }

        base.Dispose(disposing);
    }

    private static Task SendTextAsync(ClientWebSocket webSocket, string text, CancellationToken cancellationToken) =>
        webSocket.SendAsync(
            Encoding.UTF8.GetBytes(text),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);

    private void ReleaseTransports()
    {
        _lifetime?.Cancel();
        _udpClient?.Dispose();
        _udpClient = null;
        _webSocket?.Dispose();
        _webSocket = null;
        _lifetime?.Dispose();
        _lifetime = null;
        _receiveLoop = null;
        _heartbeatLoop = null;
    }

    private Task PaceAsync(int milliseconds, CancellationToken cancellationToken) =>
        milliseconds <= 0
            ? Task.CompletedTask
            : Task.Delay(TimeSpan.FromMilliseconds(milliseconds), TimeProvider, cancellationToken);

    private Task SendCommandAsync(VantaCommandId commandId, object? parameters, CancellationToken cancellationToken)
    {
        ClientWebSocket webSocket = _webSocket ?? throw new InvalidOperationException("vanta_not_connected");
        int id = Interlocked.Increment(ref _commandCounter) % 100;
        string json = JsonSerializer.Serialize(
            new { commandId = (int)commandId, id, @params = parameters },
            JsonOptions);
        return SendTextAsync(webSocket, json, cancellationToken);
    }

    private async Task TryLogoutAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_webSocket is { State: WebSocketState.Open })
            {
                await SendCommandAsync(VantaCommandId.Logout, null, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is WebSocketException or InvalidOperationException or ObjectDisposedException)
        {
            // Logout is best-effort session hygiene; a failure never masks the run outcome.
        }
    }

    private async Task HeartbeatLoopAsync(UdpClient udpClient, CancellationToken cancellationToken)
    {
        byte[] datagram = Encoding.ASCII.GetBytes(VantaProtocolStrings.HeartbeatMessage);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await udpClient.SendAsync(datagram, cancellationToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMilliseconds(_options.HeartbeatIntervalMs), TimeProvider, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (SocketException)
        {
            // Heartbeat loss surfaces via the probe's battery-status staleness.
        }
        catch (ObjectDisposedException)
        {
            // Transport torn down during disconnect.
        }
    }

    private async Task ReceiveLoopAsync(ClientWebSocket webSocket, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[16 * 1024];
        try
        {
            while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
            {
                using var message = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return;
                    }

                    message.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                HandleMessage(Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length));
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (Exception ex) when (ex is WebSocketException or ObjectDisposedException or IOException)
        {
            _runTcs?.TrySetResult("vanta_socket_lost");
            SetHealth(DeviceState.Faulted, "vanta_socket_lost");
        }
    }

    private void HandleMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (message.Contains(VantaProtocolStrings.ReadyToReceiveCommands, StringComparison.Ordinal))
        {
            _readyTcs?.TrySetResult(true);
            return;
        }

        if (message.Contains(VantaProtocolStrings.DeviceInUseByOther, StringComparison.Ordinal)
            || message.Contains(VantaProtocolStrings.OtherUserLoggedIn, StringComparison.Ordinal))
        {
            _readyTcs?.TrySetException(new DeviceConnectFailedException("vanta_device_in_use"));
            return;
        }

        VantaEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<VantaEnvelope>(message, JsonOptions);
        }
        catch (JsonException)
        {
            return; // Non-JSON firmware chatter is ignored, matching the legacy handler.
        }

        if (envelope is null || envelope.CommandId != (int)VantaCommandId.Notification)
        {
            return;
        }

        switch ((VantaNotificationId)envelope.Id)
        {
            case VantaNotificationId.BatteryStatus:
                _lastBatteryStatus = TimeProvider.GetUtcNow();
                break;
            case VantaNotificationId.ExposureStatus:
                RaiseProgress(50, "authenticating");
                break;
            case VantaNotificationId.ResultReceived:
                HandleResult(envelope.Params?.Result);
                break;
            case VantaNotificationId.ErrorDuringTest:
                _runTcs?.TrySetResult($"vanta_error:{envelope.Error?.ErrorCode ?? -1}");
                break;
            case VantaNotificationId.SystemStatus:
            default:
                break;
        }
    }

    private void HandleResult(VantaResult? result)
    {
        if (result is null)
        {
            return;
        }

        if (result.Analysis?.Final != true)
        {
            RaiseProgress(75, "authenticating");
            return;
        }

        lock (_runLock)
        {
            _finalResults.Add(result);
            _runTcs?.TrySetResult(null);
        }
    }

    private void RaiseProgress(int percent, string stage) =>
        ProgressChanged?.Invoke(this, new AnalysisProgress(percent, stage));
}
