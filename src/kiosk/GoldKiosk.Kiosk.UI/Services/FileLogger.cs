using Microsoft.Extensions.Logging;

namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// The per-category logger for <see cref="FileLoggerProvider"/>.
/// </summary>
/// <param name="category">The logger category name.</param>
/// <param name="provider">The owning provider that performs the writes.</param>
public sealed class FileLogger(string category, FileLoggerProvider provider) : ILogger
{
    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        if (!IsEnabled(logLevel))
        {
            return;
        }

        provider.Write(logLevel, category, formatter(state, exception), exception);
    }
}
