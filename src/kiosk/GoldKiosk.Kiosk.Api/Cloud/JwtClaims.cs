using System.Text.Json;

namespace GoldKiosk.Kiosk.Api.Cloud;

/// <summary>
/// Minimal, dependency-free reader for the one JWT claim the edge needs from the kiosk
/// token — <c>tenant_id</c>. The token is already trusted (issued by Cloud.Api over the
/// authenticated login), so this only base64url-decodes the payload; it does not re-verify
/// the signature. Never logs the token.
/// </summary>
internal static class JwtClaims
{
    private const string TenantIdClaim = "tenant_id";

    /// <summary>Reads the <c>tenant_id</c> claim from a JWT, or <see langword="null"/> when absent/unparseable.</summary>
    /// <param name="jwt">The compact JWT string.</param>
    /// <returns>The tenant id, or <see langword="null"/>.</returns>
    public static Guid? ReadTenantId(string jwt)
    {
        ArgumentNullException.ThrowIfNull(jwt);

        string[] parts = jwt.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            byte[] payload = Base64UrlDecode(parts[1]);
            using JsonDocument document = JsonDocument.Parse(payload);
            if (document.RootElement.TryGetProperty(TenantIdClaim, out JsonElement element)
                && element.ValueKind == JsonValueKind.String
                && Guid.TryParse(element.GetString(), out Guid tenantId))
            {
                return tenantId;
            }
        }
        catch (JsonException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }

        return null;
    }

    private static byte[] Base64UrlDecode(string value)
    {
        string normalized = value.Replace('-', '+').Replace('_', '/');
        normalized += (normalized.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty,
        };
        return Convert.FromBase64String(normalized);
    }
}
