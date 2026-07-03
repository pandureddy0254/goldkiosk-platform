namespace GoldKiosk.Kiosk.UI.Resources;

/// <summary>
/// The kiosk string catalogue — an <c>IStringLocalizer</c>-like service every piece of
/// screen copy goes through (no hardcoded strings in Razor). en-US is complete; further
/// locales are added as additional dictionaries.
/// </summary>
public interface ILocalizedStrings
{
    /// <summary>Gets the active BCP 47 locale.</summary>
    string Locale { get; }

    /// <summary>Gets the localized string for a key, or the key itself when missing.</summary>
    /// <param name="key">The catalogue key, e.g. <c>offer.accept</c>.</param>
    string this[string key] { get; }

    /// <summary>Returns whether the catalogue contains a key in the active locale chain.</summary>
    /// <param name="key">The catalogue key.</param>
    /// <returns><see langword="true"/> when the key resolves.</returns>
    bool Contains(string key);

    /// <summary>Formats a localized string with arguments using the active locale's culture.</summary>
    /// <param name="key">The catalogue key.</param>
    /// <param name="args">The format arguments.</param>
    /// <returns>The formatted string.</returns>
    string Format(string key, params object?[] args);

    /// <summary>Switches the active locale, falling back to en-US for missing keys.</summary>
    /// <param name="locale">The BCP 47 locale, e.g. <c>en-US</c>.</param>
    void SetLocale(string locale);
}
