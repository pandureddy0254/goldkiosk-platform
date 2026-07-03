using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Layout;

/// <summary>
/// The full-screen kiosk stage: a fixed 1080×1920 portrait canvas scaled to the display,
/// the root-level pointer handler that resets the idle timer, the overlay host, and the
/// central state → route navigation (screens never navigate themselves).
/// </summary>
public partial class KioskLayout : IDisposable
{
    [Inject]
    private SessionStore Store { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private FlowController Flow { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private NavigationManager Navigation { get; set; } = default!; // non-null via [Inject]

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Store.Changed += OnStoreChanged;
        Flow.Changed += OnFlowChanged;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Store.Changed -= OnStoreChanged;
        Flow.Changed -= OnFlowChanged;
        GC.SuppressFinalize(this);
    }

    private void OnActivity() => Flow.NotifyUserActivity();

    private void OnStoreChanged() => _ = InvokeAsync(() =>
    {
        string target = FlowController.RouteForState(Store.State);
        string current = "/" + Navigation.ToBaseRelativePath(Navigation.Uri);
        if (!string.Equals(current, target, StringComparison.OrdinalIgnoreCase))
        {
            Navigation.NavigateTo(target);
        }

        StateHasChanged();
    });

    private void OnFlowChanged() => _ = InvokeAsync(StateHasChanged);
}
