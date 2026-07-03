using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Protocol;
using GoldKiosk.Kiosk.Devices.Real.Transport;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real customer tray driver: motorized drawer on the stepper board (shared COM3 channel,
/// legacy <c>ld</c> open / <c>lu</c> close with <c>*</c> acknowledgement) verified against
/// the tray-closed sensor bit on <see cref="ISensorBoard"/>. Without an injected sensor
/// board the driver degrades to ack + fixed travel dwell (documented legacy 4 s re-check).
/// A verify timeout reports <see cref="TrayState.Blocked"/> — never a silent success.
/// </summary>
public sealed class RealTray : RealDeviceBase, ITray
{
    private readonly ISensorBoard? _sensorBoard;
    private readonly ConnectionOptions _connection;
    private readonly TrayOptions _options;
    private SerialPortLease? _lease;

    /// <summary>
    /// Initializes the driver with legacy-parity defaults and no sensor verify (composition
    /// wires the sensor board via the parameterized constructor in a follow-up).
    /// </summary>
    public RealTray()
        : this(null, null, null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="sensorBoard">Sensor board for end-of-travel verification; <see langword="null"/> degrades to a fixed dwell.</param>
    /// <param name="connection">Connection overrides; <see langword="null"/> uses <see cref="RealDeviceDefaults"/> (COM3, 9600).</param>
    /// <param name="options">Tray tuning; <see langword="null"/> uses defaults.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealTray(
        ISensorBoard? sensorBoard,
        ConnectionOptions? connection = null,
        TrayOptions? options = null,
        TimeProvider? timeProvider = null)
        : base(DeviceKeys.Tray, "Customer tray (stepper drive + sensor verify)", isCritical: true, timeProvider)
    {
        _sensorBoard = sensorBoard;
        _connection = (connection ?? new ConnectionOptions()).MergedWith(RealDeviceDefaults.For(DeviceKeys.Tray));
        _options = options ?? new TrayOptions();
    }

    /// <inheritdoc />
    public TrayState State { get; private set; } = TrayState.Closed;

    /// <inheritdoc />
    public Task OpenAsync(CancellationToken cancellationToken = default) =>
        MoveAsync(StepperBoardCommands.OpenTray, TrayState.Open, cancellationToken);

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default) =>
        MoveAsync(StepperBoardCommands.CloseTray, TrayState.Closed, cancellationToken);

    /// <inheritdoc />
    protected override async Task ConnectCoreAsync(CancellationToken cancellationToken)
    {
        SerialPortLease lease = SerialPortChannelPool.Acquire(_connection.Port!, _connection.BaudRate!.Value);
        try
        {
            await lease.Channel.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            lease.Dispose();
            throw;
        }

        _lease = lease;

        // Initial position from the sensor when available (legacy start-of-day recovery
        // closed an open tray; here we only report the observed state — recovery is a flow
        // decision upstream).
        if (_sensorBoard is not null)
        {
            SensorSnapshot snapshot = await _sensorBoard.ReadAsync(cancellationToken).ConfigureAwait(false);
            State = snapshot.TrayClosed ? TrayState.Closed : TrayState.Open;
        }
    }

    /// <inheritdoc />
    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        ReleaseLease();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseLease();
        }

        base.Dispose(disposing);
    }

    private void ReleaseLease()
    {
        _lease?.Dispose();
        _lease = null;
    }

    private async Task MoveAsync(string command, TrayState target, CancellationToken cancellationToken)
    {
        EnsureOperable();
        if (State == target)
        {
            return;
        }

        SerialPortChannel channel = (_lease ?? throw new InvalidOperationException("tray_not_connected")).Channel;
        SetHealth(DeviceState.Busy, target == TrayState.Open ? "Tray opening." : "Tray closing.");
        State = TrayState.Moving;
        try
        {
            await channel.ExecuteAsync(port =>
            {
                port.DiscardInBuffer();
                port.WriteLine(command);
                SerialPortChannel.WaitForAck(port, StepperBoardCommands.Ack, _options.AckTimeoutMs, TimeProvider);
                return true;
            }, cancellationToken).ConfigureAwait(false);

            bool confirmed = await VerifyPositionAsync(target, cancellationToken).ConfigureAwait(false);
            if (!confirmed)
            {
                State = TrayState.Blocked;
                SetHealth(DeviceState.Faulted, "tray_motion_timeout");
                throw new TimeoutException("tray_motion_timeout");
            }

            State = target;
            SetHealth(DeviceState.Ready);
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Ready);
            throw;
        }
        catch (TimeoutException)
        {
            throw;
        }
        catch (Exception ex)
        {
            State = TrayState.Blocked;
            SetHealth(DeviceState.Faulted, $"tray_move_failed:{ex.GetType().Name}");
            throw;
        }
    }

    private async Task<bool> VerifyPositionAsync(TrayState target, CancellationToken cancellationToken)
    {
        if (_sensorBoard is null)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(_options.FallbackTravelDelayMs), TimeProvider, cancellationToken)
                .ConfigureAwait(false);
            return true;
        }

        long started = TimeProvider.GetTimestamp();
        while (TimeProvider.GetElapsedTime(started).TotalMilliseconds < _options.MotionTimeoutMs)
        {
            SensorSnapshot snapshot = await _sensorBoard.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (target == TrayState.Closed ? snapshot.TrayClosed : !snapshot.TrayClosed)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(_options.SensorPollIntervalMs), TimeProvider, cancellationToken)
                .ConfigureAwait(false);
        }

        return false;
    }
}
