namespace GoldKiosk.Kiosk.Devices.Real.Transport;

/// <summary>
/// One driver's hold on a pooled <see cref="SerialPortChannel"/>. Disposing releases the
/// hold; the underlying port closes when the last lease across the process is released.
/// </summary>
internal sealed class SerialPortLease : IDisposable
{
    private readonly string _portName;
    private bool _disposed;

    internal SerialPortLease(string portName, SerialPortChannel channel)
    {
        _portName = portName;
        Channel = channel;
    }

    /// <summary>The shared channel (valid until this lease is disposed).</summary>
    public SerialPortChannel Channel { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SerialPortChannelPool.Release(_portName);
    }
}
