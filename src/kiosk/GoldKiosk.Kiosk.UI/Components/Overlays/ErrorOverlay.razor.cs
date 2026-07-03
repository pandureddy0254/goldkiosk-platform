using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Overlays;

/// <summary>
/// The localized error/recovery sheet. Content comes from the error catalogue keyed by
/// rejection reason codes and ProblemDetails type codes; buttons execute recovery
/// actions through the flow controller.
/// </summary>
public partial class ErrorOverlay : IDisposable
{
    [Inject]
    private FlowController Flow { get; set; } = default!; // non-null via [Inject]

    /// <inheritdoc />
    protected override void OnInitialized() => Flow.Changed += OnFlowChanged;

    /// <inheritdoc />
    public void Dispose()
    {
        Flow.Changed -= OnFlowChanged;
        GC.SuppressFinalize(this);
    }

    private void OnFlowChanged() => _ = InvokeAsync(StateHasChanged);

    private async Task OnChoiceAsync(RecoveryChoice choice) =>
        await Flow.ExecuteRecoveryAsync(choice.Action);
}
