using System.Globalization;
using System.Text.Json;
using NSec.Cryptography;

namespace GoldKiosk.Cloud.AdminPortal.Services.Licensing;

/// <summary>
/// Outcome of verifying an AIKI- license token offline against the CRM's
/// published Ed25519 public key(s). <see cref="License"/> may be populated even
/// when <see cref="Valid"/> is false (e.g. expired, revoked) so the caller can
/// surface useful detail like the actual expiry date.
/// </summary>
/// <param name="Valid">True when the token's signature, key id and expiry all check out.</param>
/// <param name="License">The decoded payload when it could be parsed, even for invalid tokens.</param>
/// <param name="Error">Human-readable reason when <paramref name="Valid"/> is false.</param>
public sealed record VerifyOutcome(bool Valid, License? License, string? Error);

/// <summary>
/// Verifies AIKI-base64url(payload).base64url(sig) license tokens against a
/// pinned or JWKS-loaded set of Ed25519 public keys. Stateless and thread-safe
/// once constructed - replace the instance, don't mutate, when keys rotate.
/// </summary>
public sealed class LicenseVerifier
{
    /// <summary>Prefix every CRM-issued license token carries.</summary>
    public const string TokenPrefix = "AIKI-";

    private readonly IReadOnlyDictionary<string, byte[]> _publicKeysByKid;

    /// <summary>
    /// Initializes a verifier over the given raw Ed25519 public keys, keyed by JWKS kid.
    /// </summary>
    /// <param name="publicKeysByKid">Raw 32-byte Ed25519 public keys keyed by kid.</param>
    public LicenseVerifier(IReadOnlyDictionary<string, byte[]> publicKeysByKid)
    {
        _publicKeysByKid = publicKeysByKid;
    }

    /// <summary>The raw public keys this verifier trusts, keyed by JWKS kid.</summary>
    public IReadOnlyDictionary<string, byte[]> PublicKeysByKid => _publicKeysByKid;

    /// <summary>
    /// Build a verifier from a JWKS document fetched from the CRM's
    /// <c>/.well-known/jwks.json</c>. Only OKP/Ed25519 keys are loaded;
    /// everything else is silently skipped.
    /// </summary>
    /// <param name="jwksJson">The JWKS JSON document.</param>
    public static LicenseVerifier FromJwks(string jwksJson)
    {
        using var doc = JsonDocument.Parse(jwksJson);
        var map = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var k in doc.RootElement.GetProperty("keys").EnumerateArray())
        {
            if (k.TryGetProperty("kty", out var kty) && kty.GetString() != "OKP")
            {
                continue;
            }

            if (k.TryGetProperty("crv", out var crv) && crv.GetString() != "Ed25519")
            {
                continue;
            }

            var kid = k.GetProperty("kid").GetString() ?? "";
            var x = k.GetProperty("x").GetString() ?? "";
            if (kid.Length == 0 || x.Length == 0)
            {
                continue;
            }

            map[kid] = Base64UrlDecode(x);
        }

        return new LicenseVerifier(map);
    }

    /// <summary>
    /// Verifies a raw AIKI- token: shape, payload decoding, key id, signature, expiry.
    /// </summary>
    /// <param name="token">The raw token pasted by the operator.</param>
    public VerifyOutcome Verify(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !token.StartsWith(TokenPrefix, StringComparison.Ordinal))
        {
            return new(false, null, "License token must start with AIKI-.");
        }

        var body = token[TokenPrefix.Length..];
        var dot = body.IndexOf('.', StringComparison.Ordinal);
        if (dot <= 0 || dot == body.Length - 1)
        {
            return new(false, null, "License token is malformed (missing payload/signature separator).");
        }

        byte[] payloadBytes;
        byte[] sig;
        try
        {
            payloadBytes = Base64UrlDecode(body[..dot]);
            sig = Base64UrlDecode(body[(dot + 1)..]);
        }
        catch (FormatException)
        {
            return new(false, null, "License token contains invalid base64url.");
        }

        License? license;
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(payloadBytes);
            license = JsonSerializer.Deserialize<License>(json);
        }
        catch (JsonException ex)
        {
            return new(false, null, "License payload is not valid JSON: " + ex.Message);
        }

        if (license is null)
        {
            return new(false, null, "License payload deserialized to null.");
        }

        if (!_publicKeysByKid.TryGetValue(license.Kid, out var pkRaw))
        {
            return new(false, license, $"Unknown signing key id '{license.Kid}'. The CRM may have rotated keys - refresh JWKS and try again.");
        }

        var pk = PublicKey.Import(SignatureAlgorithm.Ed25519, pkRaw, KeyBlobFormat.RawPublicKey);
        if (!SignatureAlgorithm.Ed25519.Verify(pk, payloadBytes, sig))
        {
            return new(false, license, "License signature does not match payload - the token has been tampered with or was issued by a different CRM.");
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (license.ExpiresAt > 0 && license.ExpiresAt < now)
        {
            return new(false, license, $"License expired on {license.ExpiresOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.");
        }

        return new(true, license, null);
    }

    /// <summary>
    /// Extract the first 8 characters of the base64url-encoded payload - the
    /// CRM's revocation list keys revoke-entries by this AIKI-XXXXXXXX prefix
    /// rather than full token (so partial keys never leak the signature).
    /// </summary>
    /// <param name="token">The raw token.</param>
    public static string? ExtractRevocationPrefix(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !token.StartsWith(TokenPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var body = token[TokenPrefix.Length..];
        return body.Length < 8 ? null : TokenPrefix + body[..8];
    }

    private static byte[] Base64UrlDecode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        var pad = padded.Length % 4;
        if (pad > 0)
        {
            padded += new string('=', 4 - pad);
        }

        return Convert.FromBase64String(padded);
    }
}
