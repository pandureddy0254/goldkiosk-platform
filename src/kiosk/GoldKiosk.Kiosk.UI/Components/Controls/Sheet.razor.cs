using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Controls;

/// <summary>
/// A bottom sheet with a frosted glass panel — used for the terms text and the offer
/// explainer. Tapping the backdrop or the close button raises <see cref="OnClose"/>.
/// </summary>
public partial class Sheet
{
    /// <summary>Gets or sets whether the sheet is showing.</summary>
    [Parameter]
    public bool Visible { get; set; }

    /// <summary>Gets or sets the sheet title.</summary>
    [Parameter]
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the close-button label.</summary>
    [Parameter]
    public string CloseLabel { get; set; } = "Close";

    /// <summary>Gets or sets the sheet content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the callback raised when the sheet asks to close.</summary>
    [Parameter]
    public EventCallback OnClose { get; set; }

    private async Task CloseAsync() => await OnClose.InvokeAsync();
}
