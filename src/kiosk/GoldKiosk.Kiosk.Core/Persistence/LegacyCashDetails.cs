using System.Text.Json.Serialization;

namespace GoldKiosk.Kiosk.Core.Persistence;

/// <summary>
/// The <c>CashDetails</c> block of the legacy-shaped <c>transactionDetails.json</c> —
/// dispensed note counts per denomination (field names and casing 1:1 with the legacy
/// parity contract §6). All zero for non-cash payouts.
/// </summary>
public sealed record LegacyCashDetails
{
    /// <summary>Count of $1 notes dispensed.</summary>
    [JsonPropertyName("One")]
    public required int One { get; init; }

    /// <summary>Count of $2 notes dispensed.</summary>
    [JsonPropertyName("Two")]
    public required int Two { get; init; }

    /// <summary>Count of $5 notes dispensed.</summary>
    [JsonPropertyName("Five")]
    public required int Five { get; init; }

    /// <summary>Count of $10 notes dispensed.</summary>
    [JsonPropertyName("Ten")]
    public required int Ten { get; init; }

    /// <summary>Count of $20 notes dispensed.</summary>
    [JsonPropertyName("Twenty")]
    public required int Twenty { get; init; }

    /// <summary>Count of $50 notes dispensed.</summary>
    [JsonPropertyName("Fifty")]
    public required int Fifty { get; init; }

    /// <summary>Count of $100 notes dispensed.</summary>
    [JsonPropertyName("Hundred")]
    public required int Hundred { get; init; }

    /// <summary>Builds the block from a denomination → count map (empty map = all zeros).</summary>
    /// <param name="bills">Denomination in major units → note count; <see langword="null"/> for non-cash.</param>
    /// <returns>The cash details block.</returns>
    public static LegacyCashDetails FromBills(IReadOnlyDictionary<int, int>? bills)
    {
        bills ??= new Dictionary<int, int>();
        return new LegacyCashDetails
        {
            One = bills.GetValueOrDefault(1),
            Two = bills.GetValueOrDefault(2),
            Five = bills.GetValueOrDefault(5),
            Ten = bills.GetValueOrDefault(10),
            Twenty = bills.GetValueOrDefault(20),
            Fifty = bills.GetValueOrDefault(50),
            Hundred = bills.GetValueOrDefault(100),
        };
    }
}
