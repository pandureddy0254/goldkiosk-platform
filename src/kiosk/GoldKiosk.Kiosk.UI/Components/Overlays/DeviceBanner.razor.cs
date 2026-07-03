using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Overlays;

/// <summary>
/// A subtle top banner shown when <c>device_health_changed</c> reports the kiosk
/// hardware as degraded or faulted.
/// </summary>
public partial class DeviceBanner : IDisposable
{
    [Inject]
    private SessionStore Store { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private ILocalizedStrings L { get; set; } = default!; // non-null via [Inject]

    /// <inheritdoc />
    protected override void OnInitialized() => Store.Changed += OnStoreChanged;

    /// <inheritdoc />
    public void Dispose()
    {
        Store.Changed -= OnStoreChanged;
        GC.SuppressFinalize(this);
    }

    private void OnStoreChanged() => _ = InvokeAsync(StateHasChanged);
}
