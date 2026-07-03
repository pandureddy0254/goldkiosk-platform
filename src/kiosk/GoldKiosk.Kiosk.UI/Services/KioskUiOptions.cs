namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// Options for the kiosk UI shell, bound from the <c>KioskUi</c> configuration section
/// (package-default <c>appsettings.json</c>; layered machine/cloud config arrives later
/// per the configuration standard). Validated eagerly at startup — the kiosk fails fast
/// on invalid configuration rather than limping.
/// </summary>
public sealed class KioskUiOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "KioskUi";

    /// <summary>Gets or sets the Kiosk API base URL (localhost-only edge API).</summary>
    public string ApiBaseUrl { get; set; } = "http://localhost:5201";

    /// <summary>Gets or sets the default BCP 47 locale for the attract loop.</summary>
    public string Locale { get; set; } = "en-US";

    /// <summary>Gets or sets how long the done screen shows before auto-returning to attract.</summary>
    public int DoneScreenSeconds { get; set; } = 15;

    /// <summary>Gets or sets the countdown window shown by the idle-timeout overlay, in seconds.</summary>
    public int IdleGraceSeconds { get; set; } = 30;

    /// <summary>Gets or sets the attract-loop word rotation cadence, in milliseconds.</summary>
    public int AttractWordCycleMs { get; set; } = 2500;

    /// <summary>
    /// Validates the options, throwing on the first invalid value so startup fails fast.
    /// </summary>
    /// <exception cref="InvalidOperationException">A configured value is invalid.</exception>
    public void Validate()
    {
        if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out Uri? uri) || !uri.IsLoopback)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{nameof(ApiBaseUrl)} must be an absolute loopback URL; the Kiosk API binds to localhost only.");
        }

        if (string.IsNullOrWhiteSpace(Locale))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(Locale)} must be a BCP 47 locale.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(DoneScreenSeconds);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(IdleGraceSeconds);
        ArgumentOutOfRangeException.ThrowIfLessThan(AttractWordCycleMs, 500);
    }
}
