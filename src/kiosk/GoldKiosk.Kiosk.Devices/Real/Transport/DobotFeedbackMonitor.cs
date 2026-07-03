using System.Net.Sockets;

namespace GoldKiosk.Kiosk.Devices.Real.Transport;

/// <summary>
/// Background reader of the Dobot CR real-time feedback stream (:30004, little-endian
/// 1440-byte packets validated by the 0x0123456789ABCDEF test value at offset 48; robot mode
/// is the long at offset 24 — layout per the legacy <c>Feedback.ParseData</c>). Provides the
/// motion-complete discipline that replaces the legacy blind <c>Task.Delay(500)</c>: after a
/// move is issued, callers wait for the mode to return to <see cref="DobotRobotMode.Enabled"/>
/// (idle), treating <see cref="DobotRobotMode.Error"/> as failure.
/// </summary>
internal sealed class DobotFeedbackMonitor : IDisposable
{
    private const int PacketSize = 1440;
    private const int RobotModeOffset = 24;
    private const int TestValueOffset = 48;
    private const ulong TestValue = 0x0123456789ABCDEF;

    private readonly TimeProvider _timeProvider;
    private CancellationTokenSource? _lifetime;
    private TcpClient? _client;
    private Task? _readLoop;
    private long _robotMode = (long)DobotRobotMode.NoController;
    private bool _disposed;

    internal DobotFeedbackMonitor(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <summary>The most recently observed robot mode.</summary>
    public DobotRobotMode CurrentMode => (DobotRobotMode)Interlocked.Read(ref _robotMode);

    /// <summary>Connects to the feedback port and starts the background reader.</summary>
    /// <param name="host">Controller IP address.</param>
    /// <param name="port">Feedback port (30004).</param>
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
        _lifetime = new CancellationTokenSource();
        _readLoop = Task.Run(() => ReadLoopAsync(client.GetStream(), _lifetime.Token), CancellationToken.None);
    }

    /// <summary>
    /// Waits for the arm to finish the motion just issued: a grace window tolerates the
    /// stream not yet showing <see cref="DobotRobotMode.Running"/> for very short moves, then
    /// idle (<see cref="DobotRobotMode.Enabled"/>) completes the wait.
    /// </summary>
    /// <param name="graceMs">Window during which idle-without-having-run is not yet trusted.</param>
    /// <param name="timeoutMs">Overall completion deadline.</param>
    /// <param name="pollIntervalMs">Mode poll interval.</param>
    /// <param name="cancellationToken">Cancels the wait (the arm still finishes safely).</param>
    /// <exception cref="InvalidOperationException">The arm entered its error mode.</exception>
    /// <exception cref="TimeoutException">The motion did not complete within the deadline.</exception>
    public async Task WaitForMotionCompleteAsync(
        int graceMs,
        int timeoutMs,
        int pollIntervalMs,
        CancellationToken cancellationToken)
    {
        long started = _timeProvider.GetTimestamp();
        bool sawRunning = false;
        while (true)
        {
            double elapsedMs = _timeProvider.GetElapsedTime(started).TotalMilliseconds;
            if (elapsedMs >= timeoutMs)
            {
                throw new TimeoutException("arm_motion_timeout");
            }

            DobotRobotMode mode = CurrentMode;
            if (mode == DobotRobotMode.Error)
            {
                throw new InvalidOperationException("arm_error_mode");
            }

            if (mode == DobotRobotMode.Running)
            {
                sawRunning = true;
            }
            else if (mode == DobotRobotMode.Enabled && (sawRunning || elapsedMs >= graceMs))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(pollIntervalMs), _timeProvider, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lifetime?.Cancel();
        _client?.Dispose();
        _lifetime?.Dispose();
    }

    private async Task ReadLoopAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[PacketSize * 3];
        int filled = 0;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(filled, buffer.Length - filled), cancellationToken)
                    .ConfigureAwait(false);
                if (read <= 0)
                {
                    break;
                }

                filled += read;
                filled = ConsumePackets(buffer, filled);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException)
        {
            // Stream torn down — the next motion wait times out and surfaces the fault.
        }

        Interlocked.Exchange(ref _robotMode, (long)DobotRobotMode.NoController);
    }

    private int ConsumePackets(byte[] buffer, int filled)
    {
        int offset = 0;
        while (filled - offset >= PacketSize)
        {
            int packetStart = -1;
            int searchLimit = filled - TestValueOffset - sizeof(ulong);
            for (int i = offset; i <= searchLimit; i++)
            {
                if (BitConverter.ToUInt16(buffer, i) == PacketSize
                    && BitConverter.ToUInt64(buffer, i + TestValueOffset) == TestValue)
                {
                    packetStart = i;
                    break;
                }
            }

            if (packetStart < 0 || filled - packetStart < PacketSize)
            {
                offset = packetStart < 0 ? Math.Max(offset, filled - PacketSize + 1) : packetStart;
                break;
            }

            Interlocked.Exchange(ref _robotMode, BitConverter.ToInt64(buffer, packetStart + RobotModeOffset));
            offset = packetStart + PacketSize;
        }

        if (offset > 0)
        {
            Buffer.BlockCopy(buffer, offset, buffer, 0, filled - offset);
            filled -= offset;
        }

        if (filled >= buffer.Length)
        {
            filled = 0;
        }

        return filled;
    }
}
