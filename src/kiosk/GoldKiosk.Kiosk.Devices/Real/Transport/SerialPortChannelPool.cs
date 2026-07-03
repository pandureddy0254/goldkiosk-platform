namespace GoldKiosk.Kiosk.Devices.Real.Transport;

/// <summary>
/// Process-wide, reference-counted pool of <see cref="SerialPortChannel"/>s keyed by port
/// name. Static state is justified here: a physical COM port is a machine-wide exclusive
/// resource, and the legacy wiring drives two logical devices (tray, volume chamber) through
/// the single stepper board on COM3 — independently constructed drivers must resolve to the
/// same channel or the second <c>Open</c> would throw. The channel closes when its last
/// lease is disposed.
/// </summary>
internal static class SerialPortChannelPool
{
    private static readonly Lock _syncRoot = new();
    private static readonly Dictionary<string, PooledChannel> _channels = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Acquires a lease on the shared channel for a port, creating it on first use.</summary>
    /// <param name="portName">The COM port name.</param>
    /// <param name="baudRate">The baud rate; must match any existing lease on the same port.</param>
    /// <returns>The lease; dispose it to release the channel.</returns>
    /// <exception cref="InvalidOperationException">The port is already pooled at a different baud rate.</exception>
    public static SerialPortLease Acquire(string portName, int baudRate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baudRate);

        lock (_syncRoot)
        {
            if (_channels.TryGetValue(portName, out PooledChannel? pooled))
            {
                if (pooled.BaudRate != baudRate)
                {
                    throw new InvalidOperationException(
                        $"Serial port '{portName}' is already shared at {pooled.BaudRate} baud; cannot re-open at {baudRate}.");
                }

                pooled.LeaseCount++;
                return new SerialPortLease(portName, pooled.Channel);
            }

            var created = new PooledChannel(new SerialPortChannel(portName, baudRate), baudRate);
            _channels[portName] = created;
            return new SerialPortLease(portName, created.Channel);
        }
    }

    /// <summary>Releases one lease; the channel is disposed when no leases remain.</summary>
    /// <param name="portName">The COM port name of the lease being released.</param>
    internal static void Release(string portName)
    {
        lock (_syncRoot)
        {
            if (!_channels.TryGetValue(portName, out PooledChannel? pooled))
            {
                return;
            }

            pooled.LeaseCount--;
            if (pooled.LeaseCount <= 0)
            {
                _channels.Remove(portName);
                pooled.Channel.Dispose();
            }
        }
    }

    private sealed class PooledChannel(SerialPortChannel channel, int baudRate)
    {
        public SerialPortChannel Channel { get; } = channel;

        public int BaudRate { get; } = baudRate;

        public int LeaseCount { get; set; } = 1;
    }
}
