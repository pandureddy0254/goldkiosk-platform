using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace GoldKiosk.Infrastructure.Identity;

/// <summary>
/// Argon2id-based password hasher for ASP.NET Core Identity.
/// Replaces the default PBKDF2 hasher (which is faster and weaker).
/// <para>
/// Output format: <c>$argon2id$v=19$m=65536,t=3,p=4$&lt;salt-b64&gt;$&lt;hash-b64&gt;</c>
/// </para>
/// </summary>
public sealed class Argon2idPasswordHasher<TUser> : IPasswordHasher<TUser> where TUser : class
{
    private const int SaltSize = 16;     // 128 bits
    private const int HashSize = 32;     // 256 bits
    private const int MemorySizeKb = 65536;  // 64 MiB
    private const int Iterations = 3;
    private const int DegreeOfParallelism = 4;
    private const string Tag = "$argon2id$v=19$m=65536,t=3,p=4$";

    /// <summary>Hash password.</summary>
    public string HashPassword(TUser user, string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = ComputeHash(password, salt);

        return string.Concat(
            Tag,
            Convert.ToBase64String(salt),
            "$",
            Convert.ToBase64String(hash));
    }

    /// <summary>Verify hashed password.</summary>
    public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(hashedPassword);
        ArgumentNullException.ThrowIfNull(providedPassword);

        if (!hashedPassword.StartsWith(Tag, StringComparison.Ordinal))
        {
            return PasswordVerificationResult.Failed;
        }

        var payload = hashedPassword.AsSpan(Tag.Length);
        var dollar = payload.IndexOf('$');
        if (dollar <= 0 || dollar == payload.Length - 1)
        {
            return PasswordVerificationResult.Failed;
        }

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(payload[..dollar].ToString());
            expected = Convert.FromBase64String(payload[(dollar + 1)..].ToString());
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }

        var actual = ComputeHash(providedPassword, salt);
        return CryptographicOperations.FixedTimeEquals(actual, expected)
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.Failed;
    }

    private static byte[] ComputeHash(string password, byte[] salt)
    {
        using var hasher = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySizeKb,
            Iterations = Iterations
        };
        return hasher.GetBytes(HashSize);
    }
}
