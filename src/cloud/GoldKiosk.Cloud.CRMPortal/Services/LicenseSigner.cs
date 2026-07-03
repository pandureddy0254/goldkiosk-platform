using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using NSec.Cryptography;

namespace GoldKiosk.Cloud.CRMPortal.Services;

/// <summary>
/// Binds the <c>License</c> configuration section. Private key material is supplied
/// exclusively via user-secrets (dev) / Key Vault (prod) — never committed.
/// </summary>
public class LicenseOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "License";

    /// <summary>Issuer string embedded in every license payload.</summary>
    public string Issuer { get; set; } = "goldkiosk.crm";

    /// <summary>Default license term in days when the payload carries no explicit expiry.</summary>
    public int DefaultTermDays { get; set; } = 365;

    /// <summary>The key id used for signing. Must have a private key in <see cref="Keys"/>.</summary>
    public string ActiveKid { get; set; } = "";

    /// <summary>Key material by kid; multiple entries support rotation.</summary>
    public Dictionary<string, LicenseKeyMaterial> Keys { get; set; } = [];
}

/// <summary>One Ed25519 keypair (raw 32-byte keys, base64). Private half is secret material.</summary>
public class LicenseKeyMaterial
{
    /// <summary>Raw Ed25519 public key, base64 (publishable — appears in the JWKS).</summary>
    public string PublicKeyBase64 { get; set; } = "";

    /// <summary>Raw Ed25519 private key, base64. Secret — user-secrets / Key Vault only.</summary>
    public string PrivateKeyBase64 { get; set; } = "";
}

/// <summary>
/// Everything the Admin Dashboard needs to know about a partner without ever
/// calling back into the CRM. Signed payload — never edit a property name or
/// existing keys will fail to verify. Add new fields at the end.
/// </summary>
public class LicensePayload
{
    /// <summary>Issuer string (<c>iss</c>).</summary>
    [JsonPropertyName("iss")]
    public string Issuer { get; set; } = "";

    /// <summary>Signing key id (<c>kid</c>).</summary>
    [JsonPropertyName("kid")]
    public string Kid { get; set; } = "";

    /// <summary>Payload schema version (<c>ver</c>).</summary>
    [JsonPropertyName("ver")]
    public int Version { get; set; } = 1;

    /// <summary>Tenant id the license is bound to (<c>tid</c>).</summary>
    [JsonPropertyName("tid")]
    public Guid TenantId { get; set; }

    /// <summary>Partner id the license is bound to (<c>pid</c>).</summary>
    [JsonPropertyName("pid")]
    public Guid PartnerId { get; set; }

    /// <summary>Licensed legal entity name (<c>co</c>).</summary>
    [JsonPropertyName("co")]
    public string LegalName { get; set; } = "";

    /// <summary>Licensed display name (<c>dn</c>).</summary>
    [JsonPropertyName("dn")]
    public string DisplayName { get; set; } = "";

    /// <summary>Commercial plan code (<c>pl</c>).</summary>
    [JsonPropertyName("pl")]
    public string PlanCode { get; set; } = "starter";

    /// <summary>Sorted, lowercased list of feature flags this license unlocks.</summary>
    [JsonPropertyName("ft")]
    public string[] Features { get; set; } = [];

    /// <summary>Maximum kiosks the license allows (<c>kc</c>).</summary>
    [JsonPropertyName("kc")]
    public int KioskCap { get; set; }

    /// <summary>Deployment region hint (<c>rg</c>).</summary>
    [JsonPropertyName("rg")]
    public string RegionHint { get; set; } = "";

    /// <summary>Primary admin email the license was issued to (<c>eml</c>).</summary>
    [JsonPropertyName("eml")]
    public string AdminEmail { get; set; } = "";

    /// <summary>Unix seconds.</summary>
    [JsonPropertyName("iat")]
    public long IssuedAt { get; set; }

    /// <summary>Unix seconds.</summary>
    [JsonPropertyName("exp")]
    public long ExpiresAt { get; set; }
}

/// <summary>A freshly signed license token plus the identifiers the CRM records.</summary>
/// <param name="Token">The full cleartext token (revealed once, never stored).</param>
/// <param name="KeyPrefix">The token's <c>AIKI-XXXXXXXX</c> prefix.</param>
/// <param name="Sha256Hex">Lowercase SHA-256 hex of the token, stored for audit/revocation.</param>
/// <param name="Payload">The signed payload.</param>
public record IssuedLicense(string Token, string KeyPrefix, string Sha256Hex, LicensePayload Payload);

/// <summary>Outcome of verifying a license token.</summary>
/// <param name="Valid">True when the signature and expiry checks passed.</param>
/// <param name="Payload">The decoded payload, when parseable.</param>
/// <param name="Error">Failure detail when <paramref name="Valid"/> is false.</param>
public record VerifyResult(bool Valid, LicensePayload? Payload, string? Error);

/// <summary>Signs and verifies offline Ed25519 license tokens.</summary>
public interface ILicenseSigner
{
    /// <summary>Signs <paramref name="payload"/> with the active key and returns the token.</summary>
    /// <param name="payload">Payload to sign; issuer/kid/timestamps are stamped in place.</param>
    IssuedLicense Issue(LicensePayload payload);

    /// <summary>Verifies a token against the configured public keys.</summary>
    /// <param name="token">The full <c>AIKI-</c> token string.</param>
    VerifyResult Verify(string token);

    /// <summary>Raw public key bytes by kid, for the JWKS endpoint.</summary>
    IReadOnlyDictionary<string, byte[]> PublicKeysByKid { get; }

    /// <summary>The kid currently used for signing.</summary>
    string ActiveKid { get; }
}

/// <summary>Default <see cref="ILicenseSigner"/> backed by NSec's Ed25519 implementation.</summary>
public class LicenseSigner : ILicenseSigner
{
    /// <summary>Every token starts with this prefix.</summary>
    public const string TokenPrefix = "AIKI-";

    private static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    private readonly LicenseOptions _opts;
    private readonly Dictionary<string, Key> _privateKeys = [];
    private readonly Dictionary<string, PublicKey> _publicKeys = [];
    private readonly Dictionary<string, byte[]> _publicKeyBytes = [];

    /// <summary>Imports the configured key material and validates the active kid.</summary>
    /// <param name="opts">Bound <see cref="LicenseOptions"/> (private keys from user-secrets/Key Vault).</param>
    /// <exception cref="InvalidOperationException">The active kid has no matching private key.</exception>
    public LicenseSigner(IOptions<LicenseOptions> opts)
    {
        _opts = opts.Value;
        var algo = SignatureAlgorithm.Ed25519;

        foreach (var (kid, mat) in _opts.Keys)
        {
            if (!string.IsNullOrWhiteSpace(mat.PublicKeyBase64))
            {
                var pkBytes = Convert.FromBase64String(mat.PublicKeyBase64);
                _publicKeys[kid] = PublicKey.Import(algo, pkBytes, KeyBlobFormat.RawPublicKey);
                _publicKeyBytes[kid] = pkBytes;
            }

            if (!string.IsNullOrWhiteSpace(mat.PrivateKeyBase64))
            {
                var skBytes = Convert.FromBase64String(mat.PrivateKeyBase64);
                var keyOpts = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.None };
                _privateKeys[kid] = Key.Import(algo, skBytes, KeyBlobFormat.RawPrivateKey, keyOpts);
            }
        }

        if (string.IsNullOrWhiteSpace(_opts.ActiveKid) || !_privateKeys.ContainsKey(_opts.ActiveKid))
        {
            throw new InvalidOperationException(
                $"License:ActiveKid='{_opts.ActiveKid}' has no matching private key in License:Keys.");
        }
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, byte[]> PublicKeysByKid => _publicKeyBytes;

    /// <inheritdoc/>
    public string ActiveKid => _opts.ActiveKid;

    /// <inheritdoc/>
    public IssuedLicense Issue(LicensePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        payload.Issuer = _opts.Issuer;
        payload.Kid = _opts.ActiveKid;
        if (payload.IssuedAt == 0)
        {
            payload.IssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        if (payload.ExpiresAt == 0)
        {
            payload.ExpiresAt = DateTimeOffset.UtcNow.AddDays(_opts.DefaultTermDays).ToUnixTimeSeconds();
        }

        var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, Json);
        var sk = _privateKeys[_opts.ActiveKid];
        var sig = SignatureAlgorithm.Ed25519.Sign(sk, payloadBytes);

        var token = TokenPrefix +
                    Base64Url(payloadBytes) + "." +
                    Base64Url(sig);

        // sha256 + prefix used by the CRM for audit / revocation lookups.
        var sha = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var hex = Convert.ToHexStringLower(sha);
        var prefix = token[..Math.Min(13, token.Length)]; // "AIKI-XXXXXXXX"
        return new IssuedLicense(token, prefix, hex, payload);
    }

    /// <inheritdoc/>
    public VerifyResult Verify(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || !token.StartsWith(TokenPrefix, StringComparison.Ordinal))
        {
            return new VerifyResult(false, null, "Token must start with AIKI-.");
        }

        var body = token[TokenPrefix.Length..];
        var dot = body.IndexOf('.', StringComparison.Ordinal);
        if (dot <= 0 || dot == body.Length - 1)
        {
            return new VerifyResult(false, null, "Token is malformed (expected one '.' separator).");
        }

        byte[] payloadBytes;
        byte[] sig;
        try
        {
            payloadBytes = Base64UrlDecode(body[..dot]);
            sig = Base64UrlDecode(body[(dot + 1)..]);
        }
        catch (FormatException ex)
        {
            return new VerifyResult(false, null, "Token contains invalid base64url: " + ex.Message);
        }

        LicensePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<LicensePayload>(payloadBytes);
        }
        catch (JsonException ex)
        {
            return new VerifyResult(false, null, "Token payload is not valid JSON: " + ex.Message);
        }

        if (payload is null)
        {
            return new VerifyResult(false, null, "Token payload deserialized to null.");
        }

        if (string.IsNullOrWhiteSpace(payload.Kid) || !_publicKeys.TryGetValue(payload.Kid, out var pk))
        {
            return new VerifyResult(false, payload, $"Unknown signing key id '{payload.Kid}'.");
        }

        if (!SignatureAlgorithm.Ed25519.Verify(pk, payloadBytes, sig))
        {
            return new VerifyResult(false, payload, "Signature is invalid for the stated payload.");
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (payload.ExpiresAt > 0 && payload.ExpiresAt < now)
        {
            return new VerifyResult(false, payload, "License expired on " +
                DateTimeOffset.FromUnixTimeSeconds(payload.ExpiresAt).ToString("u", System.Globalization.CultureInfo.InvariantCulture) + ".");
        }

        return new VerifyResult(true, payload, null);
    }

    // ── Base64Url helpers (no padding — JWT-compatible) ─────────────────────

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

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
