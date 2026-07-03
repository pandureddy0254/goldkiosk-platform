namespace GoldKiosk.Cloud.CRMPortal.Models.ViewModels;

/// <summary>One-time reveal of a freshly issued license token (cleartext is never stored).</summary>
public class ActivationKeyRevealViewModel
{
    /// <summary>The full cleartext activation token — shown once, then only its hash survives.</summary>
    public string ActivationKey { get; set; } = string.Empty;

    /// <summary>The token's <c>AIKI-XXXXXXXX</c> prefix used for support/revocation lookups.</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>When the embedded license expires.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>The authoritative tenant id the token was signed for.</summary>
    public Guid TenantId { get; set; }

    /// <summary>The partner the token belongs to.</summary>
    public Guid PartnerId { get; set; }

    /// <summary>Partner display name for the reveal card.</summary>
    public string PartnerName { get; set; } = string.Empty;

    /// <summary>Admin email the token was delivered to.</summary>
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>Deployment region code recorded with the tenant.</summary>
    public string RegionCode { get; set; } = "ae-1";

    /// <summary>Abbreviated SHA-256 of the token for visual verification.</summary>
    public string Sha256Prefix { get; set; } = string.Empty;
}
