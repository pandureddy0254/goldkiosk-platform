using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Core.Options;

/// <summary>
/// Options for the <c>Kiosk</c> configuration section — the machine's identity and the
/// session-level policy knobs. Plain POCO — this library has no framework references, so
/// the Kiosk.Api host binds it with
/// <c>ValidateDataAnnotations().ValidateOnStart()</c>. Values resolve through the standard
/// config layering (configuration-and-operations §1); identity facts come from the
/// machine's <c>kiosk-settings.json</c> at provisioning.
/// </summary>
public sealed class KioskOptions
{
    /// <summary>The configuration section name this options class binds from.</summary>
    public const string SectionName = "Kiosk";

    /// <summary>The kiosk's fleet identifier, e.g. <c>GK-DEV-01</c>.</summary>
    [Required(AllowEmptyStrings = false)]
    public string KioskId { get; set; } = string.Empty;

    /// <summary>The store the kiosk is installed in, e.g. <c>STORE-DEV</c>.</summary>
    [Required(AllowEmptyStrings = false)]
    public string StoreId { get; set; } = string.Empty;

    /// <summary>
    /// Root folder for per-transaction folders (ADR 0002). Production default is
    /// <c>%ProgramData%\GoldKiosk\transactions</c>; dev overrides to a relative path.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string TransactionRoot { get; set; } = DefaultTransactionRoot();

    /// <summary>How long an offer stays locked once made, in seconds.</summary>
    [Range(30, 3600)]
    public int OfferTtlSeconds { get; set; } = 600;

    /// <summary>Idle seconds before an active session auto-aborts.</summary>
    [Range(15, 3600)]
    public int IdleTimeoutSeconds { get; set; } = 90;

    /// <summary>The current terms-and-conditions version customers must accept.</summary>
    [Required(AllowEmptyStrings = false)]
    public string TermsVersion { get; set; } = string.Empty;

    private static string DefaultTransactionRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "GoldKiosk",
            "transactions");
}
