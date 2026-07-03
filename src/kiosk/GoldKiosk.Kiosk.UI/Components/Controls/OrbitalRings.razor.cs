using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Controls;

/// <summary>
/// The orbital scan visual: a soft cyan core with two counter-rotating arc rings.
/// The design system never shows a loading spinner — AI work is expressed as orbital
/// motion instead.
/// </summary>
public partial class OrbitalRings
{
    /// <summary>Gets or sets whether the compact variant renders.</summary>
    [Parameter]
    public bool Small { get; set; }
}
