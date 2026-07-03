using System.Windows;
using System.Windows.Input;
using GoldKiosk.Kiosk.UI.Logging;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.Logging;

namespace GoldKiosk.Kiosk.UI;

/// <summary>
/// Full-screen kiosk shell window hosting the BlazorWebView. Chrome-less, maximized,
/// topmost in Release; the cursor stays visible for touch. A DEBUG-only Escape+F12
/// gesture exits during development.
/// </summary>
public partial class MainWindow : Window
{
    private readonly KioskHubClient _hubClient;
    private readonly ILogger<MainWindow> _logger;

    /// <summary>Initializes the kiosk shell window and wires the Blazor root component.</summary>
    /// <param name="services">The application service provider for the BlazorWebView.</param>
    /// <param name="hubClient">The kiosk hub client, started once the window loads.</param>
    /// <param name="logger">The logger.</param>
    public MainWindow(IServiceProvider services, KioskHubClient hubClient, ILogger<MainWindow> logger)
    {
        _hubClient = hubClient;
        _logger = logger;
        InitializeComponent();

        WebView.Services = services;
        WebView.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = typeof(Components.Routes),
        });

#if !DEBUG
        Topmost = true;
#endif
    }

    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            await _hubClient.StartAsync();
        }
        catch (Exception ex)
        {
            // async void WPF event handler: never let an exception escape.
            _logger.HubStartFailed(ex);
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
#if DEBUG
        if (e.Key == Key.F12 && Keyboard.IsKeyDown(Key.Escape))
        {
            _logger.DebugExitGesture();
            Application.Current.Shutdown();
        }
#endif
    }
}
