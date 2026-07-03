using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace GoldKiosk.Kiosk.UI.Components.Controls;

/// <summary>
/// A simple on-screen touch keyboard (QWERTY and numeric layouts). Emits keystrokes to
/// the owning screen; never invokes the OS keyboard.
/// </summary>
public partial class TouchKeyboard
{
    private static readonly IReadOnlyList<IReadOnlyList<string>> _textRows =
    [
        ["q", "w", "e", "r", "t", "y", "u", "i", "o", "p"],
        ["a", "s", "d", "f", "g", "h", "j", "k", "l"],
        ["⇧", "z", "x", "c", "v", "b", "n", "m", "⌫"],
        ["-", "_", ".", "space", "'", "@", "&"],
    ];

    private static readonly IReadOnlyList<IReadOnlyList<string>> _emailRows =
    [
        ["q", "w", "e", "r", "t", "y", "u", "i", "o", "p"],
        ["a", "s", "d", "f", "g", "h", "j", "k", "l"],
        ["⇧", "z", "x", "c", "v", "b", "n", "m", "⌫"],
        ["@", ".", "-", "_", "+", ".com"],
    ];

    private static readonly IReadOnlyList<IReadOnlyList<string>> _numericRows =
    [
        ["1", "2", "3"],
        ["4", "5", "6"],
        ["7", "8", "9"],
        ["+", "0", "⌫"],
    ];

    private bool _shift;

    /// <summary>Gets or sets the keyboard layout.</summary>
    [Parameter]
    public TouchKeyboardLayout Layout { get; set; }

    /// <summary>Gets or sets the callback raised with each key's text.</summary>
    [Parameter]
    public EventCallback<string> OnKey { get; set; }

    /// <summary>Gets or sets the callback raised on backspace.</summary>
    [Parameter]
    public EventCallback OnBackspace { get; set; }

    private IReadOnlyList<IReadOnlyList<string>> Rows => Layout switch
    {
        TouchKeyboardLayout.Numeric => _numericRows,
        TouchKeyboardLayout.Email => _emailRows,
        _ => _textRows,
    };

    private string Display(string key) =>
        _shift && key.Length == 1 ? key.ToUpper(CultureInfo.InvariantCulture) : key;

    private void ToggleShift() => _shift = !_shift;

    private async Task KeyAsync(string key)
    {
        string text = Display(key);
        if (_shift && key.Length == 1)
        {
            _shift = false;
        }

        await OnKey.InvokeAsync(text);
    }

    private async Task BackspaceAsync() => await OnBackspace.InvokeAsync();
}
