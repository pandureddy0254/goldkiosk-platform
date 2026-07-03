using System.Security.Cryptography;

namespace GoldKiosk.Kiosk.Core.Sessions;

/// <summary>
/// Generates ULID-style identifiers (Crockford base32, time-ordered) with the wire
/// prefixes used across the Kiosk API: <c>ses_</c>, <c>off_</c>, <c>rcp_</c>.
/// The timestamp component flows through the injected <see cref="TimeProvider"/>.
/// </summary>
public static class KioskIdGenerator
{
    private const string CrockfordAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>Creates a new session identifier, e.g. <c>ses_01JZC7H2Q4R8TN5M9W3VXK6P0D</c>.</summary>
    /// <param name="timeProvider">Time source for the identifier's timestamp component.</param>
    /// <returns>The new identifier.</returns>
    public static string NewSessionId(TimeProvider timeProvider) => "ses_" + NewUlid(timeProvider);

    /// <summary>Creates a new offer identifier, e.g. <c>off_01JZC7NQ2B8XW4E5T6Y7U8I9O0</c>.</summary>
    /// <param name="timeProvider">Time source for the identifier's timestamp component.</param>
    /// <returns>The new identifier.</returns>
    public static string NewOfferId(TimeProvider timeProvider) => "off_" + NewUlid(timeProvider);

    /// <summary>Creates a new receipt identifier, e.g. <c>rcp_01JZC7ZM4K2Q…</c>.</summary>
    /// <param name="timeProvider">Time source for the identifier's timestamp component.</param>
    /// <returns>The new identifier.</returns>
    public static string NewReceiptId(TimeProvider timeProvider) => "rcp_" + NewUlid(timeProvider);

    private static string NewUlid(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        long unixMs = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();
        Span<byte> bytes = stackalloc byte[16];
        bytes[0] = (byte)(unixMs >> 40);
        bytes[1] = (byte)(unixMs >> 32);
        bytes[2] = (byte)(unixMs >> 24);
        bytes[3] = (byte)(unixMs >> 16);
        bytes[4] = (byte)(unixMs >> 8);
        bytes[5] = (byte)unixMs;
        RandomNumberGenerator.Fill(bytes[6..]);

        UInt128 value = 0;
        foreach (byte b in bytes)
        {
            value = (value << 8) | b;
        }

        Span<char> chars = stackalloc char[26];
        for (int i = 25; i >= 0; i--)
        {
            chars[i] = CrockfordAlphabet[(int)(value & 31)];
            value >>= 5;
        }

        return new string(chars);
    }
}
