namespace GoldKiosk.Infrastructure.Entities.Kiosk;

/// <summary>
/// Maps onto <c>kiosk.kiosk_security_tokens</c>. SHA-256 hashed kiosk-issued
/// bearer tokens. The raw token is only known at issue time; subsequent reads
/// can only present <c>TokenHash</c>.
/// </summary>
public sealed class KioskSecurityToken
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the kiosk id.</summary>
    public Guid KioskId { get; set; }
    /// <summary>Gets or sets the token hash.</summary>
    public byte[] TokenHash { get; set; } = Array.Empty<byte>();
    /// <summary>Gets or sets the issued at.</summary>
    public DateTimeOffset IssuedAt { get; set; }
    /// <summary>Gets or sets the expires at.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
    /// <summary>Gets or sets the revoked at.</summary>
    public DateTimeOffset? RevokedAt { get; set; }
    /// <summary>Gets or sets the last used at.</summary>
    public DateTimeOffset? LastUsedAt { get; set; }
    /// <summary>Gets or sets the issued by user id.</summary>
    public Guid? IssuedByUserId { get; set; }
}
