using System.Text.Json.Serialization;

namespace GoldKiosk.Kiosk.Core.Persistence;

/// <summary>
/// The legacy-shaped <c>transactionDetails.json</c> record written at settlement — field
/// names and casing preserved 1:1 with the legacy parity contract §6 so downstream tooling
/// and operator habits survive cut-over (ADR 0002). Restricted content (licence number,
/// fingerprint hash) is withheld as empty placeholders pending the at-rest encryption layer.
/// </summary>
public sealed record LegacyTransactionDetails
{
    /// <summary>The transaction identifier.</summary>
    [JsonPropertyName("StoreTransactionId")]
    public required Guid StoreTransactionId { get; init; }

    /// <summary>The machine identifier (legacy name for the kiosk id).</summary>
    [JsonPropertyName("DispenserId")]
    public required string DispenserId { get; init; }

    /// <summary>The store identifier.</summary>
    [JsonPropertyName("StoreId")]
    public required string StoreId { get; init; }

    /// <summary>Driving-licence number — withheld placeholder (restricted PII, ADR 0002).</summary>
    [JsonPropertyName("LicenseNo")]
    public required string LicenseNo { get; init; }

    /// <summary>The customer block.</summary>
    [JsonPropertyName("Customer")]
    public required LegacyCustomer Customer { get; init; }

    /// <summary>Fingerprint hash — withheld placeholder (biometric PII, ADR 0002).</summary>
    [JsonPropertyName("FingerPrintHash")]
    public required string FingerPrintHash { get; init; }

    /// <summary>Whether this record is a refund; always <see langword="false"/> for kiosk sales.</summary>
    [JsonPropertyName("IsRefund")]
    public required bool IsRefund { get; init; }

    /// <summary>The transacted items (the kiosk flow carries one item per session).</summary>
    [JsonPropertyName("Items")]
    public required IReadOnlyList<LegacyItem> Items { get; init; }

    /// <summary>The total paid out in major currency units.</summary>
    [JsonPropertyName("TotalPayout")]
    public required decimal TotalPayout { get; init; }

    /// <summary>The dispensed note counts per denomination.</summary>
    [JsonPropertyName("CashDetails")]
    public required LegacyCashDetails CashDetails { get; init; }

    /// <summary>Whether the transaction is a pawn.</summary>
    [JsonPropertyName("IsPawn")]
    public required bool IsPawn { get; init; }

    /// <summary>Whether a crypto payout was selected; feature-flagged off by default.</summary>
    [JsonPropertyName("IsCrypto")]
    public required bool IsCrypto { get; init; }

    /// <summary>The selected crypto currency, when <see cref="IsCrypto"/> is set.</summary>
    [JsonPropertyName("CryptoCurrencySelected")]
    public required string? CryptoCurrencySelected { get; init; }
}
