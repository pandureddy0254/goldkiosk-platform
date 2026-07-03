using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;

namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// Minimal local-first file logging for the UI shell: daily rolling text files under
/// <c>%ProgramData%\GoldKiosk\logs\app\</c> (falling back to local app data when the
/// machine folder is not writable). Logging must never crash the kiosk — all I/O
/// failures are swallowed. Never log PII or secrets.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly TimeProvider _timeProvider;
    private readonly object _gate = new();
    private string? _directory;
    private bool _directoryUnavailable;

    /// <summary>Initializes the provider.</summary>
    /// <param name="timeProvider">The time provider for timestamps and file rolling.</param>
    public FileLoggerProvider(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, this);

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing held open between writes.
    }

    internal void Write(LogLevel level, string category, string message, Exception? exception)
    {
        try
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            string line = string.Create(
                CultureInfo.InvariantCulture,
                $"{now:yyyy-MM-dd HH:mm:ss.fff}Z [{level}] {category} {message}");
            if (exception is not null)
            {
                line += Environment.NewLine + exception;
            }

            lock (_gate)
            {
                string? directory = ResolveDirectory();
                if (directory is null)
                {
                    return;
                }

                string path = Path.Combine(
                    directory,
                    string.Create(CultureInfo.InvariantCulture, $"kiosk-ui-{now:yyyyMMdd}.log"));
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            // Deliberately swallowed: logging must never take the kiosk down.
        }
    }

    private string? ResolveDirectory()
    {
        if (_directory is not null)
        {
            return _directory;
        }

        if (_directoryUnavailable)
        {
            return null;
        }

        string programData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "GoldKiosk", "logs", "app");
        string localAppData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GoldKiosk", "logs", "app");

        foreach (string candidate in new[] { programData, localAppData })
        {
            try
            {
                Directory.CreateDirectory(candidate);
                _directory = candidate;
                return _directory;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Try the next candidate.
            }
        }

        _directoryUnavailable = true;
        return null;
    }
}
