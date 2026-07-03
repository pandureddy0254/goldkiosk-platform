using System.Text;
using GoldKiosk.Cloud.Api.Logging;
using GoldKiosk.Cloud.Api.Options;
using GoldKiosk.Contracts.V1.Cloud.Auth;
using GoldKiosk.Infrastructure.Data;
using GoldKiosk.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace GoldKiosk.Cloud.Api.Auth;

/// <summary>
/// Code + PIN kiosk authentication issuing a short-lived HMAC-SHA256 JWT. The PIN is
/// verified against the Argon2id hash stored on <c>kiosk.kiosks.pin_hash</c>; claims carry
/// <c>sub</c> (kiosk id), <c>kiosk_code</c>, <c>tenant_id</c> and <c>role = kiosk</c>.
/// Adapted from platform2's <c>KioskAuthService</c> with centralized key handling.
/// </summary>
/// <param name="db">The platform database.</param>
/// <param name="passwordHasher">The Argon2id hasher (registered for <see cref="AppUser"/>).</param>
/// <param name="jwtOptions">The JWT issuance settings.</param>
/// <param name="timeProvider">The clock.</param>
/// <param name="logger">The host logger.</param>
public sealed class KioskAuthService(
    AppDbContext db,
    IPasswordHasher<AppUser> passwordHasher,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider,
    ILogger<KioskAuthService> logger) : IKioskAuthService
{
    /// <inheritdoc />
    public async Task<KioskLoginResponse?> LoginAsync(
        string code, string pin, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(pin);

        Infrastructure.Entities.Kiosk.Kiosk? kiosk = await db.Kiosks
            .AsNoTracking()
            .Where(k => k.Code == code && k.IsActive && k.Status != "decommissioned")
            .FirstOrDefaultAsync(cancellationToken);

        if (kiosk is null || string.IsNullOrEmpty(kiosk.PinHash))
        {
            logger.KioskLoginRejected(code);
            return null;
        }

        PasswordVerificationResult result =
            passwordHasher.VerifyHashedPassword(new AppUser(), kiosk.PinHash, pin);
        if (result == PasswordVerificationResult.Failed)
        {
            logger.KioskLoginRejected(code);
            return null;
        }

        JwtOptions jwt = jwtOptions.Value;
        DateTimeOffset now = timeProvider.GetUtcNow();
        DateTimeOffset expiresAt = now.AddMinutes(jwt.TokenLifetimeMinutes);
        string token = BuildJwt(jwt, kiosk, now, expiresAt);

        logger.KioskLoggedIn(kiosk.Code, kiosk.Id);
        return new KioskLoginResponse(token, expiresAt, kiosk.Id, kiosk.FriendlyName);
    }

    private static string BuildJwt(
        JwtOptions jwt,
        Infrastructure.Entities.Kiosk.Kiosk kiosk,
        DateTimeOffset issuedAt,
        DateTimeOffset expiresAt)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [KioskClaimTypes.Subject] = kiosk.Id.ToString(),
                [KioskClaimTypes.KioskCode] = kiosk.Code,
                [KioskClaimTypes.TenantId] = kiosk.TenantId.ToString(),
                [KioskClaimTypes.Role] = KioskClaimTypes.KioskRoleValue,
            },
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
