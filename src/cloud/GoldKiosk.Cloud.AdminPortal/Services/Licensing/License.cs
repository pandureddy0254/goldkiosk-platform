using System.Text.Json.Serialization;

namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>
/// The CRM-issued license payload. JSON property names are wire-locked - never
/// rename them or existing AIKI- tokens will fail to deserialize.
/// </summary>
public class License
{
    /// <summary>Issuer identifier (<c>iss</c>).</summary>
    [JsonPropertyName("iss")]
    public string Issuer { get; set; } = "";

    /// <summary>Signing key id (<c>kid</c>) used to look up the verification key.</summary>
    [JsonPropertyName("kid")]
    public string Kid { get; set; } = "";

    /// <summary>License payload schema version (<c>ver</c>).</summary>
    [JsonPropertyName("ver")]
    public int Version { get; set; } = 1;

    /// <summary>Tenant the license is bound to (<c>tid</c>).</summary>
    [JsonPropertyName("tid")]
    public Guid TenantId { get; set; }

    /// <summary>Partner that owns the tenant (<c>pid</c>).</summary>
    [JsonPropertyName("pid")]
    public Guid PartnerId { get; set; }

    /// <summary>Registered legal name of the licensee (<c>co</c>).</summary>
    [JsonPropertyName("co")]
    public string LegalName { get; set; } = "";

    /// <summary>Display name shown in portal branding (<c>dn</c>).</summary>
    [JsonPropertyName("dn")]
    public string DisplayName { get; set; } = "";

    /// <summary>Commercial plan code (<c>pl</c>).</summary>
    [JsonPropertyName("pl")]
    public string PlanCode { get; set; } = "starter";

    /// <summary>Feature codes enabled by this license (<c>ft</c>).</summary>
    [JsonPropertyName("ft")]
    public string[] Features { get; set; } = [];

    /// <summary>Maximum number of kiosks the tenant may register (<c>kc</c>).</summary>
    [JsonPropertyName("kc")]
    public int KioskCap { get; set; }

    /// <summary>Data-residency region hint (<c>rg</c>).</summary>
    [JsonPropertyName("rg")]
    public string RegionHint { get; set; } = "";

    /// <summary>Administrative contact email embedded by the CRM (<c>eml</c>).</summary>
    [JsonPropertyName("eml")]
    public string AdminEmail { get; set; } = "";

    /// <summary>Unix-seconds issue timestamp (<c>iat</c>).</summary>
    [JsonPropertyName("iat")]
    public long IssuedAt { get; set; }

    /// <summary>Unix-seconds expiry timestamp (<c>exp</c>); 0 = never expires.</summary>
    [JsonPropertyName("exp")]
    public long ExpiresAt { get; set; }

    /// <summary><see cref="ExpiresAt"/> as a <see cref="DateTimeOffset"/>.</summary>
    [JsonIgnore]
    public DateTimeOffset ExpiresOn => DateTimeOffset.FromUnixTimeSeconds(ExpiresAt);

    /// <summary><see cref="IssuedAt"/> as a <see cref="DateTimeOffset"/>.</summary>
    [JsonIgnore]
    public DateTimeOffset IssuedOn => DateTimeOffset.FromUnixTimeSeconds(IssuedAt);

    /// <summary>True when <paramref name="code"/> is in <see cref="Features"/> (case-insensitive).</summary>
    public bool HasFeature(string code) =>
        Features.Contains(code, StringComparer.OrdinalIgnoreCase);
}
