using System.Windows;

namespace GoldKiosk.Kiosk.UI;

/// <summary>
/// Full-screen kiosk shell window hosting the BlazorWebView.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes the kiosk shell window.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }
}
