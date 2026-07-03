using System.Text.Json.Serialization;

namespace GoldKiosk.Kiosk.Core.Persistence;

/// <summary>
/// The <c>Customer</c> block of the legacy-shaped <c>transactionDetails.json</c>
/// (field names and casing 1:1 with the legacy parity contract §6). Empty strings stand in
/// while KYC capture is pending or withheld.
/// </summary>
public sealed record LegacyCustomer
{
    /// <summary>The customer's phone number, or empty when not captured.</summary>
    [JsonPropertyName("Phone")]
    public required string Phone { get; init; }

    /// <summary>The customer's given name, or empty when not captured.</summary>
    [JsonPropertyName("First")]
    public required string First { get; init; }

    /// <summary>The customer's family name, or empty when not captured.</summary>
    [JsonPropertyName("Last")]
    public required string Last { get; init; }

    /// <summary>The customer's date of birth (<c>yyyy-MM-dd</c>), or empty when not captured.</summary>
    [JsonPropertyName("DOB")]
    public required string Dob { get; init; }

    /// <summary>The customer's email address, or empty when not captured.</summary>
    [JsonPropertyName("Email")]
    public required string Email { get; init; }
}
