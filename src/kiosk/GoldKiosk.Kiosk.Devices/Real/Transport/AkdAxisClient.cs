using System.Net.Sockets;
using System.Text;

namespace GoldKiosk.Kiosk.Devices.Real.Transport;

/// <summary>
/// Kollmorgen AKD servo drive client — ASCII commands over the drive's telnet port
/// (legacy default 192.168.0.11:23). Command set per the legacy linear-axis path:
/// <c>DRV.CLRFAULTS</c>, <c>DRV.EN</c>, <c>MT.MOVE n</c>, <c>HOME.MOVE</c>, with completion
/// polled from <c>DRV.MOTIONSTAT</c> (bit 0 = motion task active) instead of blind waits.
/// </summary>
internal sealed class AkdAxisClient : IDisposable
{
    private const string MotionStatusCommand = "DRV.MOTIONSTAT";
    private const int MotionTaskActiveBit = 0x1;

    private readonly TimeProvider _timeProvider;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _disposed;

    internal AkdAxisClient(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>Connects to the drive's telnet endpoint.</summary>
    /// <param name="host">Drive IP address.</param>
    /// <param name="port">Telnet port (23).</param>
    /// <param name="cancellationToken">Cancels the connect.</param>
    public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        var client = new TcpClient { NoDelay = true };
        try
        {
            await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            client.Dispose();
            throw;
        }

        _client = client;
        _stream = client.GetStream();
    }

    /// <summary>Clears faults and enables the drive (<c>DRV.CLRFAULTS</c>, <c>DRV.EN</c>).</summary>
    /// <param name="cancellationToken">Cancels the exchange.</param>
    public async Task EnableAsync(CancellationToken cancellationToken)
    {
        await SendCommandAsync("DRV.CLRFAULTS", cancellationToken).ConfigureAwait(false);
        await SendCommandAsync("DRV.EN", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs a stored motion task (<c>MT.MOVE n</c>) and waits for completion.</summary>
    /// <param name="taskNumber">The drive-stored motion task number.</param>
    /// <param name="timeoutMs">Completion deadline.</param>
    /// <param name="pollIntervalMs">Status poll interval.</param>
    /// <param name="cancellationToken">Cancels waiting (the axis finishes its move safely).</param>
    public async Task MoveTaskAsync(int taskNumber, int timeoutMs, int pollIntervalMs, CancellationToken cancellationToken)
    {
        await SendCommandAsync(
            string.Create(System.Globalization.CultureInfo.InvariantCulture, $"MT.MOVE {taskNumber}"),
            cancellationToken).ConfigureAwait(false);
        await WaitForMotionCompleteAsync(timeoutMs, pollIntervalMs, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs the homing move (<c>HOME.MOVE</c>) and waits for completion.</summary>
    /// <param name="timeoutMs">Completion deadline.</param>
    /// <param name="pollIntervalMs">Status poll interval.</param>
    /// <param name="cancellationToken">Cancels waiting.</param>
    public async Task HomeAsync(int timeoutMs, int pollIntervalMs, CancellationToken cancellationToken)
    {
        await SendCommandAsync("HOME.MOVE", cancellationToken).ConfigureAwait(false);
        await WaitForMotionCompleteAsync(timeoutMs, pollIntervalMs, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stream?.Dispose();
        _client?.Dispose();
    }

    private async Task WaitForMotionCompleteAsync(int timeoutMs, int pollIntervalMs, CancellationToken cancellationToken)
    {
        long started = _timeProvider.GetTimestamp();
        while (_timeProvider.GetElapsedTime(started).TotalMilliseconds < timeoutMs)
        {
            string response = await SendCommandAsync(MotionStatusCommand, cancellationToken).ConfigureAwait(false);
            if (TryParseFirstInteger(response, out int status) && (status & MotionTaskActiveBit) == 0)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(pollIntervalMs), _timeProvider, cancellationToken)
                .ConfigureAwait(false);
        }

        throw new TimeoutException("akd_motion_timeout");
    }

    private async Task<string> SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        NetworkStream stream = _stream ?? throw new InvalidOperationException("akd_not_connected");

        byte[] payload = Encoding.ASCII.GetBytes(command + "\r\n");
        await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);

        // The drive echoes the command and replies line-wise, ending with its "-->" prompt.
        // Read whatever arrives within a short window and return it trimmed.
        var response = new StringBuilder();
        byte[] buffer = new byte[512];
        long started = _timeProvider.GetTimestamp();
        while (_timeProvider.GetElapsedTime(started).TotalMilliseconds < 2000)
        {
            if (!stream.DataAvailable)
            {
                if (response.Length > 0)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(25), _timeProvider, cancellationToken)
                    .ConfigureAwait(false);
                continue;
            }

            int read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read <= 0)
            {
                break;
            }

            response.Append(Encoding.ASCII.GetString(buffer, 0, read));
        }

        return response.ToString().Trim();
    }

    private static bool TryParseFirstInteger(string response, out int value)
    {
        foreach (string token in response.Split(
                     [' ', '\r', '\n', '\t', '>'],
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(token, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out value))
            {
                return true;
            }
        }

        value = 0;
        return false;
    }
}
