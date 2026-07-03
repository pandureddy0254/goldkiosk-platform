using GoldKiosk.Kiosk.UI.Components.Controls;
using GoldKiosk.Kiosk.UI.Resources;
using GoldKiosk.Kiosk.UI.Services;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Screens;

/// <summary>
/// Analysis (Obsidian analysis export): orbital scan rings and the three-step checklist
/// driven by <c>analysis_progress</c> stages. No internals — weight, karat, percentages —
/// are ever shown. Live-agent escalation renders through the agent overlay.
/// </summary>
public partial class AnalysisScreen : IDisposable
{
    private static readonly string[] _stageOrder = ["item_detected", "authenticating", "pricing"];

    [Inject]
    private SessionStore Store { get; set; } = default!; // non-null via [Inject]

    [Inject]
    private ILocalizedStrings L { get; set; } = default!; // non-null via [Inject]

    private static IReadOnlyList<string> Stages => _stageOrder;

    /// <inheritdoc />
    protected override void OnInitialized() => Store.Changed += OnStoreChanged;

    /// <inheritdoc />
    public void Dispose()
    {
        Store.Changed -= OnStoreChanged;
        GC.SuppressFinalize(this);
    }

    private void OnStoreChanged() => _ = InvokeAsync(StateHasChanged);

    private ChecklistState StateFor(int index)
    {
        // item_detected reads as completed (past tense), so the active row is the next one.
        int current = Store.AnalysisStage is { } stage ? Array.IndexOf(_stageOrder, stage) : -1;
        int active = current switch
        {
            < 0 => 0,
            0 => 1,
            _ => current,
        };

        if (index < active)
        {
            return ChecklistState.Done;
        }

        return index == active ? ChecklistState.Active : ChecklistState.Pending;
    }
}
