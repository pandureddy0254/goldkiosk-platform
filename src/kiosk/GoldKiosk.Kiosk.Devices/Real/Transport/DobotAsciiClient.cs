using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace GoldKiosk.Kiosk.Devices.Real.Transport;

/// <summary>
/// Request/reply client for the Dobot CR ASCII protocol (dashboard :29999 and motion
/// :30003). Commands look like <c>EnableRobot()</c> / <c>MovJ(x,y,z,r,0,0)</c>; replies are
/// <c>errorId,{payload},command;</c>. Replaces the legacy fire-and-forget
/// <c>SendData</c>-without-reply path on the motion port — every command here waits for and
/// validates its reply.
/// </summary>
internal sealed class DobotAsciiClient : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private TcpClient? _client;
    private NetworkStream? _stream;
    private bool _disposed;

    /// <summary>Connects to one Dobot protocol port.</summary>
    /// <param name="host">Controller IP address.</param>
    /// <param name="port">Protocol port (29999 dashboard, 30003 motion).</param>
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

    /// <summary>Sends one command and returns the raw reply.</summary>
    /// <param name="command">The ASCII command including parentheses.</param>
    /// <param name="timeoutMs">Reply deadline.</param>
    /// <param name="cancellationToken">Cancels the exchange.</param>
    /// <returns>The raw reply string.</returns>
    /// <exception cref="TimeoutException">No reply arrived within the deadline.</exception>
    public async Task<string> SendAsync(string command, int timeoutMs, CancellationToken cancellationToken)
    {
        NetworkStream stream = _stream ?? throw new InvalidOperationException("dobot_not_connected");

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            byte[] payload = Encoding.UTF8.GetBytes(command);
            await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);

            using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            byte[] buffer = new byte[1024];
            try
            {
                int read = await stream.ReadAsync(buffer, linked.Token).ConfigureAwait(false);
                return Encoding.UTF8.GetString(buffer, 0, read);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"dobot_reply_timeout:{command}");
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Parses the leading error id of a reply (<c>0,{...},cmd;</c>). Returns -1 when the
    /// reply does not start with an integer (raw firmware chatter).
    /// </summary>
    /// <param name="reply">The raw reply.</param>
    /// <returns>The error id; 0 means accepted.</returns>
    public static int ParseErrorId(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            return -1;
        }

        int comma = reply.IndexOf(',', StringComparison.Ordinal);
        string head = (comma < 0 ? reply : reply[..comma]).Trim();
        return int.TryParse(head, NumberStyles.Integer, CultureInfo.InvariantCulture, out int errorId)
            ? errorId
            : -1;
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
        _gate.Dispose();
    }
}
