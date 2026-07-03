using System.Text;
using GoldKiosk.Cloud.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GoldKiosk.Cloud.Api.Auth;

/// <summary>
/// Wires <see cref="JwtBearerOptions"/> from the validated <see cref="JwtOptions"/>:
/// symmetric-key validation, raw (unmapped) claim names, and <c>role</c>/<c>sub</c> as
/// the role/name claim types so the kiosk policy evaluates the token's own claims.
/// </summary>
/// <param name="jwtOptions">The validated JWT settings.</param>
public sealed class ConfigureJwtBearerOptions(IOptions<JwtOptions> jwtOptions)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    /// <inheritdoc />
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
        {
            Configure(options);
        }
    }

    /// <inheritdoc />
    public void Configure(JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        JwtOptions jwt = jwtOptions.Value;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = KioskClaimTypes.Subject,
            RoleClaimType = KioskClaimTypes.Role,
        };
    }
}
