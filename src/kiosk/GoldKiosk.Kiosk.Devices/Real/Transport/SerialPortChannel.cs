using System.IO.Ports;

namespace GoldKiosk.Kiosk.Devices.Real.Transport;

/// <summary>
/// Thread-safe owner of one physical serial port. Every exchange (write + reads that belong
/// together) runs as one atomic unit under the channel's gate, so two drivers sharing a port
/// (tray + volume chamber on the stepper board's COM3) can never interleave protocol traffic.
/// Blocking serial I/O executes on the thread pool; reads are always bounded by
/// <see cref="SerialPort.ReadTimeout"/> set per exchange — the legacy infinite-read hang is
/// structurally impossible.
/// </summary>
internal sealed class SerialPortChannel : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SerialPort _port;
    private bool _disposed;

    internal SerialPortChannel(string portName, int baudRate)
    {
        _port = new SerialPort
        {
            PortName = portName,
            BaudRate = baudRate,
            DataBits = 8,
            StopBits = StopBits.One,
            Parity = Parity.None,
            Handshake = Handshake.None,
            NewLine = "\n",
        };
    }

    /// <summary>The owned port name (e.g. <c>COM3</c>).</summary>
    public string PortName => _port.PortName;

    /// <summary>Opens the port if not already open.</summary>
    /// <param name="cancellationToken">Cancels waiting for the channel gate.</param>
    public Task OpenAsync(CancellationToken cancellationToken) =>
        ExecuteAsync(port =>
        {
            if (!port.IsOpen)
            {
                port.Open();
            }

            return true;
        }, cancellationToken);

    /// <summary>
    /// Runs one atomic protocol exchange while holding the channel gate. The lambda receives
    /// the open port and may set <see cref="SerialPort.ReadTimeout"/> for its own reads.
    /// </summary>
    /// <typeparam name="TResult">The exchange result type.</typeparam>
    /// <param name="exchange">The synchronous exchange body (runs on the thread pool).</param>
    /// <param name="cancellationToken">Cancels waiting for the gate / starting the exchange (in-flight blocking reads end via their timeout).</param>
    /// <returns>The exchange result.</returns>
    public async Task<TResult> ExecuteAsync<TResult>(Func<SerialPort, TResult> exchange, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(exchange);
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await Task.Run(() => exchange(_port), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Blocks inside an exchange until the board sends the acknowledgement character, or
    /// throws <see cref="TimeoutException"/>. Individual reads are bounded by the port's
    /// <see cref="SerialPort.ReadTimeout"/>; the overall deadline is enforced with
    /// <paramref name="timeProvider"/> timestamps.
    /// </summary>
    /// <param name="port">The open port (inside an <see cref="ExecuteAsync{TResult}"/> body).</param>
    /// <param name="ack">The expected acknowledgement character (stepper board <c>*</c>).</param>
    /// <param name="timeoutMs">Overall deadline in milliseconds.</param>
    /// <param name="timeProvider">Time source for the deadline.</param>
    /// <exception cref="TimeoutException">No acknowledgement arrived within the deadline.</exception>
    public static void WaitForAck(SerialPort port, char ack, int timeoutMs, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(timeProvider);

        long started = timeProvider.GetTimestamp();
        int previousTimeout = port.ReadTimeout;
        port.ReadTimeout = Math.Max(50, Math.Min(timeoutMs, 1000));
        try
        {
            while (timeProvider.GetElapsedTime(started).TotalMilliseconds < timeoutMs)
            {
                try
                {
                    if (port.ReadChar() == ack)
                    {
                        return;
                    }
                }
                catch (TimeoutException)
                {
                    // Per-read timeout — keep polling until the overall deadline.
                }
            }
        }
        finally
        {
            port.ReadTimeout = previousTimeout;
        }

        throw new TimeoutException($"serial_ack_timeout:{port.PortName}");
    }

    /// <summary>Closes and releases the port. Called by the pool when the last lease is released.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _port.Dispose();
        _gate.Dispose();
    }
}
