using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Protocol;
using GoldKiosk.Kiosk.Devices.Real.Transport;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real MT-SICS scale driver over RS-232 (legacy: COM5, 9600 8-N-1, <c>Q</c> weight /
/// <c>Z</c> zero commands). Legacy bugs deliberately fixed: robust invariant-culture token
/// parse instead of <c>Substring(3, 11)</c> (see <see cref="MtSicsWeightParser"/>),
/// configurable settle delay instead of a hard-coded 2 s wait, finite read timeout instead of
/// an infinite blocking read, and the stability flag surfaced from the response.
/// </summary>
public sealed class RealScale : RealDeviceBase, IScale
{
    private readonly ConnectionOptions _connection;
    private readonly ScaleOptions _options;
    private SerialPortLease? _lease;

    /// <summary>Initializes the driver with legacy-parity defaults (COM5, 9600).</summary>
    public RealScale()
        : this(null, null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="connection">Connection overrides; <see langword="null"/> uses <see cref="RealDeviceDefaults"/>.</param>
    /// <param name="options">Scale tuning; <see langword="null"/> uses defaults.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealScale(ConnectionOptions? connection, ScaleOptions? options = null, TimeProvider? timeProvider = null)
        : base(DeviceKeys.Scale, "Precision scale (MT-SICS, RS-232)", isCritical: true, timeProvider)
    {
        _connection = (connection ?? new ConnectionOptions()).MergedWith(RealDeviceDefaults.For(DeviceKeys.Scale));
        _options = options ?? new ScaleOptions();
    }

    /// <inheritdoc />
    public async Task<WeightReading> GetWeightAsync(CancellationToken cancellationToken = default)
    {
        EnsureOperable();
        SetHealth(DeviceState.Busy, "Reading weight.");
        try
        {
            if (_options.SettleDelayMs > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(_options.SettleDelayMs), TimeProvider, cancellationToken)
                    .ConfigureAwait(false);
            }

            string response = await ExchangeAsync(_options.WeightCommand, readResponse: true, cancellationToken)
                .ConfigureAwait(false);
            if (!MtSicsWeightParser.TryParse(response, out WeightReading reading))
            {
                throw new InvalidOperationException("scale_response_unparseable");
            }

            SetHealth(DeviceState.Ready);
            return reading;
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Ready);
            throw;
        }
        catch (Exception ex)
        {
            SetHealth(DeviceState.Faulted, $"scale_read_failed:{ex.GetType().Name}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ZeroAsync(CancellationToken cancellationToken = default)
    {
        EnsureOperable();
        SetHealth(DeviceState.Busy, "Zeroing scale.");
        try
        {
            await ExchangeAsync(_options.ZeroCommand, readResponse: false, cancellationToken).ConfigureAwait(false);
            if (_options.ZeroSettleDelayMs > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(_options.ZeroSettleDelayMs), TimeProvider, cancellationToken)
                    .ConfigureAwait(false);
            }

            SetHealth(DeviceState.Ready);
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Ready);
            throw;
        }
        catch (Exception ex)
        {
            SetHealth(DeviceState.Faulted, $"scale_zero_failed:{ex.GetType().Name}");
            throw;
        }
    }

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
    }

    /// <inheritdoc />
    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        ReleaseLease();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<DeviceProbeResult> ProbeCoreAsync(CancellationToken cancellationToken)
    {
        if (_lease is null)
        {
            return new DeviceProbeResult(false, 0, Health.Detail ?? "Scale is not connected.");
        }

        string response = await ExchangeAsync(_options.WeightCommand, readResponse: true, cancellationToken)
            .ConfigureAwait(false);
        return MtSicsWeightParser.TryParse(response, out _)
            ? new DeviceProbeResult(true, 0, "Scale responded with a parseable weight.")
            : new DeviceProbeResult(false, 0, "scale_response_unparseable");
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

    private Task<string> ExchangeAsync(string command, bool readResponse, CancellationToken cancellationToken)
    {
        SerialPortChannel channel = (_lease ?? throw new InvalidOperationException("scale_not_connected")).Channel;
        return channel.ExecuteAsync(port =>
        {
            port.ReadTimeout = _options.ReadTimeoutMs;
            port.DiscardInBuffer();
            port.Write(command + "\r\n");
            if (!readResponse)
            {
                return string.Empty;
            }

            try
            {
                return port.ReadLine();
            }
            catch (TimeoutException)
            {
                throw new TimeoutException($"scale_read_timeout:{port.PortName}");
            }
        }, cancellationToken);
    }
}
