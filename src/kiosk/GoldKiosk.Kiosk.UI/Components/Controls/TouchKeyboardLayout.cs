namespace GoldKiosk.Kiosk.UI.Components.Controls;

/// <summary>The on-screen keyboard layout (the OS keyboard is never used on the kiosk).</summary>
public enum TouchKeyboardLayout
{
    /// <summary>QWERTY letters with a symbols row.</summary>
    Text,

    /// <summary>QWERTY tuned for email entry (adds <c>@</c>, <c>.</c>, common TLD keys).</summary>
    Email,

    /// <summary>Numeric pad for phone and bank numbers.</summary>
    Numeric,
}
