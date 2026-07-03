using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.Api.Options;

/// <summary>
/// Kiosk JWT issuance/validation settings. The signing key is a secret: dev via
/// <c>dotnet user-secrets set "Jwt:Key" …</c>, prod via Key Vault — never in appsettings.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Gets the symmetric HMAC-SHA256 signing key (min 32 chars). Secret — from user-secrets/Key Vault.</summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(32)]
    public required string Key { get; init; }

    /// <summary>Gets the token issuer.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = "goldkiosk-cloud";

    /// <summary>Gets the token audience.</summary>
    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = "goldkiosk-kiosk";

    /// <summary>Gets the kiosk token lifetime in minutes.</summary>
    [Range(5, 1440)]
    public int TokenLifetimeMinutes { get; init; } = 60;
}
