using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Api.Cloud;

/// <summary>
/// Options for the <c>Cloud</c> configuration section — how the edge reaches Cloud.Api.
/// Disabled by default so the offline/MSIX demo path is byte-identical to a kiosk with no
/// cloud configured. Identity facts (<see cref="KioskCode"/>) come from the machine's
/// <c>kiosk-settings.json</c>; the PIN is a secret and arrives only through user-secrets /
/// Key Vault (never a committed file). Bound with
/// <c>ValidateDataAnnotations().ValidateOnStart()</c>; the cross-field rules apply only when
/// <see cref="Enabled"/> is set, so a disabled kiosk never fails startup on cloud config.
/// </summary>
public sealed class CloudOptions : IValidatableObject
{
    /// <summary>The configuration section name this options class binds from.</summary>
    public const string SectionName = "Cloud";

    /// <summary>The default Cloud.Api base address (the fixed dev launch port).</summary>
    public const string DefaultBaseUrl = "http://localhost:5101";

    /// <summary>
    /// Whether the edge talks to Cloud.Api at all. <see langword="false"/> (default) keeps the
    /// kiosk on the local mock pricing + no cloud sync — the offline demo path.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>The Cloud.Api base URL, e.g. <c>http://localhost:5101</c>.</summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>The kiosk's fleet code used at machine login, e.g. <c>GK-MUM-004</c>.</summary>
    public string KioskCode { get; set; } = string.Empty;

    /// <summary>
    /// The kiosk PIN used at machine login. Secret — supplied via
    /// <c>dotnet user-secrets set "Cloud:KioskPin" &lt;value&gt;</c> in dev and Key Vault in
    /// prod; never written to a committed configuration file.
    /// </summary>
    public string KioskPin { get; set; } = string.Empty;

    /// <summary>How often the provisioning poll refreshes identity/status, in seconds.</summary>
    [Range(30, 86_400)]
    public int PollIntervalSeconds { get; set; } = 300;

    /// <summary>Per-request timeout in seconds; the offer path degrades to the mock on timeout.</summary>
    [Range(1, 120)]
    public int RequestTimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// How often the transaction outbox worker scans for un-forwarded transactions, in
    /// seconds. The backoff grows this on repeated failures up to a cap.
    /// </summary>
    [Range(5, 3600)]
    public int UploadIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to forward non-PII item images alongside the transaction record. Off by
    /// default: Cloud.Api exposes no transaction-image endpoint yet (see the follow-up in the
    /// gateway), so image forwarding stays inert until that lands.
    /// </summary>
    public bool UploadImages { get; set; }

    /// <summary>
    /// Folder for the last-good provisioning cache so the kiosk boots provisioned when the
    /// cloud is unreachable. Defaults to <c>%ProgramData%\GoldKiosk\cache\config</c>
    /// (configuration-and-operations §1 layer 3).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string CacheRoot { get; set; } = DefaultCacheRoot();

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enabled)
        {
            yield break;
        }

        if (string.IsNullOrWhiteSpace(BaseUrl)
            || !Uri.TryCreate(BaseUrl, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            yield return new ValidationResult(
                "Cloud:BaseUrl must be an absolute http(s) URL when Cloud:Enabled is true.",
                [nameof(BaseUrl)]);
        }

        if (string.IsNullOrWhiteSpace(KioskCode))
        {
            yield return new ValidationResult(
                "Cloud:KioskCode is required when Cloud:Enabled is true.",
                [nameof(KioskCode)]);
        }

        if (string.IsNullOrWhiteSpace(KioskPin))
        {
            yield return new ValidationResult(
                "Cloud:KioskPin is required when Cloud:Enabled is true (set it via user-secrets).",
                [nameof(KioskPin)]);
        }
    }

    private static string DefaultCacheRoot() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "GoldKiosk",
            "cache",
            "config");
}
