namespace GoldKiosk.Infrastructure.Entities.Identity;

/// <summary>
/// MFA enrolments for a user. A user can have multiple methods
/// (e.g. TOTP + WebAuthn) with one flagged <c>IsPrimary</c>.
/// </summary>
public sealed class AppUserMfa
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the user id.</summary>
    public Guid UserId { get; set; }

    /// <summary><c>totp</c> | <c>webauthn</c> | <c>sms</c>.</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Gets or sets the secret enc.</summary>
    public byte[]? SecretEnc { get; set; }
    /// <summary>Gets or sets the public key enc.</summary>
    public byte[]? PublicKeyEnc { get; set; }

    /// <summary>Gets or sets a value indicating whether is primary.</summary>
    public bool IsPrimary { get; set; }
    /// <summary>Gets or sets the enrolled at.</summary>
    public DateTimeOffset EnrolledAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Gets or sets the last used at.</summary>
    public DateTimeOffset? LastUsedAt { get; set; }
    /// <summary>Gets or sets the revoked at.</summary>
    public DateTimeOffset? RevokedAt { get; set; }
}
