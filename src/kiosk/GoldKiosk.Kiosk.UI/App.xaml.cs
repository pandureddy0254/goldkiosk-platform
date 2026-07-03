using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using GoldKiosk.Kiosk.UI.Logging;
using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GoldKiosk.Kiosk.UI;

/// <summary>
/// WPF shell application hosting the Blazor Hybrid kiosk UI. Composition root for the
/// edge UI: configuration, dependency injection, and the global exception handlers that
/// keep the kiosk alive.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _services;
    private ILogger<App>? _logger;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        KioskUiOptions options = configuration.GetSection(KioskUiOptions.SectionName).Get<KioskUiOptions>()
            ?? new KioskUiOptions();
        options.Validate(); // Fail fast on invalid config — the kiosk never limps.

        _services = BuildServices(options);
        _logger = _services.GetRequiredService<ILogger<App>>();
        _logger.UiStarting(options.ApiBaseUrl);

        // Packaged kiosk: the shell owns the edge API lifecycle (MSIX can't host a service).
        // Blocks briefly until the API answers health so the first screen has a live backend.
        _services.GetRequiredService<LocalApiHost>().StartAsync().GetAwaiter().GetResult();

        MainWindow window = _services.GetRequiredService<MainWindow>();
        window.Show();
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.UiExiting(e.ApplicationExitCode);

        // KioskHubClient is IAsyncDisposable-only, so the container must be disposed
        // asynchronously. OnExit is synchronous and the app is terminating — a bounded
        // blocking wait here cannot deadlock the (already stopping) dispatcher.
        _services?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(3));

        base.OnExit(e);
    }

    private static ServiceProvider BuildServices(KioskUiOptions options)
    {
        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
#if DEBUG
        services.AddBlazorWebViewDeveloperTools();
#endif
        services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddProvider(new FileLoggerProvider(TimeProvider.System));
        });

        services.AddSingleton(options);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<LocalApiHost>();
        services.AddSingleton<ILocalizedStrings, LocalizedStrings>();
        services.AddSingleton(_ => new HttpClient
        {
            BaseAddress = new Uri(options.ApiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(30),
        });
        services.AddSingleton<KioskApiClient>();
        services.AddSingleton<KioskHubClient>();
        services.AddSingleton<SessionStore>();
        services.AddSingleton<FlowController>();
        services.AddSingleton<MainWindow>();
        return services.BuildServiceProvider();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.DispatcherException(e.Exception);
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        _logger?.DomainException(e.ExceptionObject as Exception, e.IsTerminating);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.UnobservedTaskException(e.Exception);
        e.SetObserved();
    }
}
