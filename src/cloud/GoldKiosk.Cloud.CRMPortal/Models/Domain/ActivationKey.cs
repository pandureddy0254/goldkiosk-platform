using System.ComponentModel.DataAnnotations.Schema;

namespace GoldKiosk.Cloud.CRMPortal.Models.Domain;

/// <summary>
/// Maps to <c>crm.activation_keys</c> — the audit record of an issued offline Ed25519
/// license token. Only the SHA-256 hash is stored; the cleartext token is shown once.
/// </summary>
public class ActivationKey
{
    /// <summary>Primary key (server-generated uuid).</summary>
    public Guid Id { get; set; }

    /// <summary>The tenant the key was issued for.</summary>
    public Guid TenantId { get; set; }

    /// <summary>First 13 characters of the token (<c>AIKI-XXXXXXXX</c>) for support lookups.</summary>
    public string KeyPrefix { get; set; } = string.Empty;

    /// <summary>SHA-256 hex hash of the full token.</summary>
    public string KeyHash { get; set; } = string.Empty;

    /// <summary>Email the key was delivered to (citext column).</summary>
    public string IssuedToEmail { get; set; } = string.Empty;

    /// <summary>When the key was issued (DB default).</summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>When the embedded license expires.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>When the key was consumed by an Admin Dashboard activation, if ever.</summary>
    public DateTime? ConsumedAt { get; set; }

    /// <summary>When the key was revoked, if ever.</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Reason recorded at revocation.</summary>
    public string? RevokedReason { get; set; }

    /// <summary>Row creation timestamp (DB default; never written by the app).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Derived display state: consumed | revoked | expired | pending.</summary>
    [NotMapped]
    public string State =>
        ConsumedAt.HasValue ? "consumed" :
        RevokedAt.HasValue ? "revoked" :
        ExpiresAt < DateTime.UtcNow ? "expired" : "pending";
}
