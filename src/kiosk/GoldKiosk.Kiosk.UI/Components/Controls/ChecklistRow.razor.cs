using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Controls;

/// <summary>
/// One row of a progress checklist (analysis stages, identity steps): a state disc plus
/// a label, matching the Obsidian export treatment.
/// </summary>
public partial class ChecklistRow
{
    /// <summary>Gets or sets the row label.</summary>
    [Parameter]
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the row state.</summary>
    [Parameter]
    public ChecklistState State { get; set; }

    private string StateClass => State switch
    {
        ChecklistState.Done => "done",
        ChecklistState.Active => "active",
        ChecklistState.Failed => "failed",
        _ => "pending",
    };
}
