using System.Net;

namespace GoldKiosk.Infrastructure.Entities.Identity;

/// <summary>
/// Active sign-in sessions. The session cookie holds only the <see cref="Id"/>;
/// <see cref="TokenHash"/> is the SHA-256 of the actual bearer token —
/// the raw token is never stored.
/// </summary>
public sealed class AppUserSession
{
    /// <summary>Gets or sets the id.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the user id.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the token hash.</summary>
    public byte[] TokenHash { get; set; } = Array.Empty<byte>();

    /// <summary>Gets or sets the issued at.</summary>
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Gets or sets the expires at.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
    /// <summary>Gets or sets the last seen at.</summary>
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Gets or sets the revoked at.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Gets or sets the source IP.</summary>
    public IPAddress? SourceIp { get; set; }
    /// <summary>Gets or sets the user agent.</summary>
    public string? UserAgent { get; set; }
}
