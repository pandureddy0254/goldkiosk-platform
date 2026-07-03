using System.Globalization;

namespace GoldKiosk.Kiosk.UI.Resources;

/// <summary>
/// Static-dictionary string catalogue. Locales register a dictionary each; lookups fall
/// back to en-US, then to the key itself (so a missing key is visible, never a crash).
/// </summary>
public sealed class LocalizedStrings : ILocalizedStrings
{
    private const string DefaultLocale = "en-US";

    private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> _catalogues =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [DefaultLocale] = StringsEnUs.Values,
        };

    private IReadOnlyDictionary<string, string> _active = StringsEnUs.Values;
    private CultureInfo _culture = CultureInfo.GetCultureInfo(DefaultLocale);

    /// <inheritdoc />
    public string Locale { get; private set; } = DefaultLocale;

    /// <inheritdoc />
    public string this[string key] =>
        _active.TryGetValue(key, out string? value)
            ? value
            : StringsEnUs.Values.TryGetValue(key, out string? fallback) ? fallback : key;

    /// <inheritdoc />
    public bool Contains(string key) => _active.ContainsKey(key) || StringsEnUs.Values.ContainsKey(key);

    /// <inheritdoc />
    public string Format(string key, params object?[] args) =>
        string.Format(_culture, this[key], args);

    /// <inheritdoc />
    public void SetLocale(string locale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locale);
        Locale = locale;
        _active = _catalogues.TryGetValue(locale, out IReadOnlyDictionary<string, string>? catalogue)
            ? catalogue
            : StringsEnUs.Values;
        try
        {
            _culture = CultureInfo.GetCultureInfo(locale);
        }
        catch (CultureNotFoundException)
        {
            _culture = CultureInfo.GetCultureInfo(DefaultLocale);
        }
    }
}
