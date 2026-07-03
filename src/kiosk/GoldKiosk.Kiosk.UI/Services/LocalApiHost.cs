using System.Diagnostics;
using System.IO;
using System.Net.Http;
using GoldKiosk.Kiosk.UI.Logging;
using Microsoft.Extensions.Logging;

namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// Launches and supervises the edge Kiosk API as a child process for the packaged kiosk
/// (the MSIX "logon-launched" model: MSIX cannot host a Windows service, so the UI shell
/// owns the API lifecycle). Starts the process, waits for it to answer health, and stops
/// it on shutdown. A full watchdog with restart-on-crash is a later ticket.
/// </summary>
public sealed class LocalApiHost : IDisposable
{
    private readonly KioskUiOptions _options;
    private readonly ILogger<LocalApiHost> _logger;
    private readonly TimeProvider _timeProvider;
    private Process? _process;

    /// <summary>Initializes the local API host.</summary>
    /// <param name="options">The shell options carrying the launch flag, executable path, and API URL.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">Time source for the health-wait loop.</param>
    public LocalApiHost(KioskUiOptions options, ILogger<LocalApiHost> logger, TimeProvider timeProvider)
    {
        _options = options;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Starts the API process if <see cref="KioskUiOptions.LaunchLocalApi"/> is set and the
    /// API is not already answering, then waits until it is healthy or the timeout elapses.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the health wait.</param>
    /// <returns>A task that completes once the API is healthy or the wait ends.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.LaunchLocalApi)
        {
            return;
        }

        using var probe = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        if (await IsHealthyAsync(probe, cancellationToken).ConfigureAwait(false))
        {
            _logger.LocalApiAlreadyRunning(_options.ApiBaseUrl);
            return;
        }

        string exePath = Path.Combine(AppContext.BaseDirectory, _options.LocalApiPath);
        if (!File.Exists(exePath))
        {
            _logger.LocalApiExecutableMissing(exePath);
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // Bind the edge API to the shell's configured loopback URL, and run it in the
        // environment that maps the health/diagnostic endpoints the shell polls.
        startInfo.Environment["ASPNETCORE_URLS"] = _options.ApiBaseUrl;
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

        _process = Process.Start(startInfo);
        _logger.LocalApiStarting(exePath);

        for (var attempt = 0; attempt < 40; attempt++)
        {
            if (await IsHealthyAsync(probe, cancellationToken).ConfigureAwait(false))
            {
                _logger.LocalApiHealthy(_options.ApiBaseUrl);
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), _timeProvider, cancellationToken).ConfigureAwait(false);
        }

        _logger.LocalApiHealthTimedOut(_options.ApiBaseUrl);
    }

    private async Task<bool> IsHealthyAsync(HttpClient probe, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await probe
                .GetAsync($"{_options.ApiBaseUrl.TrimEnd('/')}/health", cancellationToken)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_process is null)
        {
            return;
        }

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(3000);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already gone — nothing to stop.
        }
        finally
        {
            _process.Dispose();
            _process = null;
        }
    }
}
