using System.Windows;

namespace GoldKiosk.Kiosk.UI;

/// <summary>
/// Transparent branded startup splash shown while the shell launches and health-checks the
/// edge API. Displays the enlarged GoldKiosk mark and a status line that ends on
/// "GoldKiosk App is ready" before the kiosk window is revealed.
/// </summary>
public partial class SplashWindow : Window
{
    /// <summary>Initializes the splash window.</summary>
    public SplashWindow()
    {
        InitializeComponent();
    }

    /// <summary>Updates the status line under the logo.</summary>
    /// <param name="text">The status text to show.</param>
    public void SetStatus(string text)
    {
        StatusText.Text = text;
    }
}
