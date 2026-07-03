using System.Globalization;
using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Overlays;

/// <summary>
/// The idle-timeout overlay: a countdown ring with resume/abort choices. Shows when the
/// local idle timer (or the server's <c>idle_warning</c>) enters the grace window; on
/// expiry or "No" the session aborts with reason <c>timeout</c>/<c>user_cancel</c>.
/// </summary>
public partial class TimeoutOverlay : IDisposable
{
    [Inject]
    private FlowController Flow { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private ILocalizedStrings L { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private KioskUiOptions Options { get; set; } = default!; // non-null via [Inject]

    /// <inheritdoc />
    protected override void OnInitialized() => Flow.Changed += OnFlowChanged;

    /// <inheritdoc />
    public void Dispose()
    {
        Flow.Changed -= OnFlowChanged;
        GC.SuppressFinalize(this);
    }

    private void OnFlowChanged() => _ = InvokeAsync(StateHasChanged);

    private void Resume() => Flow.ResumeSession();

    private async Task AbortAsync() => await Flow.AbortAsync("timeout");

    private string RingStyle(int seconds)
    {
        const double circumference = 2 * Math.PI * 52;
        double fraction = Math.Clamp((double)seconds / Options.IdleGraceSeconds, 0, 1);
        double offset = circumference * (1 - fraction);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"stroke-dasharray:{circumference:F1};stroke-dashoffset:{offset:F1}");
    }
}
