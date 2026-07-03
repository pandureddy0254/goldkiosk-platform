using System.Text.Json.Serialization;

namespace GoldKiosk.Kiosk.Core.Persistence;

/// <summary>
/// One entry of the <c>Items</c> array in the legacy-shaped <c>transactionDetails.json</c>
/// (field names and casing 1:1 with the legacy parity contract §6). Fields the new pipeline
/// does not yet produce (temperature, tier pricing) carry zero/empty placeholders.
/// </summary>
public sealed record LegacyItem
{
    /// <summary>Ambient temperature at analysis; placeholder 0 until wired.</summary>
    [JsonPropertyName("Temperature")]
    public required decimal Temperature { get; init; }

    /// <summary>Item weight in grams.</summary>
    [JsonPropertyName("Weight")]
    public required decimal Weight { get; init; }

    /// <summary>Item weight in troy pennyweight.</summary>
    [JsonPropertyName("DWTWeight")]
    public required decimal DwtWeight { get; init; }

    /// <summary>Computed karat from the XRF gold percentage.</summary>
    [JsonPropertyName("Karat")]
    public required decimal Karat { get; init; }

    /// <summary>Precious-metal content in percent.</summary>
    [JsonPropertyName("MetalPercentage")]
    public required decimal MetalPercentage { get; init; }

    /// <summary>Market spot price reference; placeholder 0 until cloud pricing lands.</summary>
    [JsonPropertyName("MarketPrice")]
    public required decimal MarketPrice { get; init; }

    /// <summary>Legacy tier price per pennyweight; placeholder 0.</summary>
    [JsonPropertyName("TierPricePerDWT")]
    public required decimal TierPricePerDwt { get; init; }

    /// <summary>Legacy unit price per pennyweight; placeholder 0.</summary>
    [JsonPropertyName("UnitPricePerDWT")]
    public required decimal UnitPricePerDwt { get; init; }

    /// <summary>Impurity summary; placeholder empty string.</summary>
    [JsonPropertyName("Impurities")]
    public required string Impurities { get; init; }

    /// <summary>The offered price in major currency units.</summary>
    [JsonPropertyName("OfferPrice")]
    public required decimal OfferPrice { get; init; }

    /// <summary>Whether the customer accepted the offer.</summary>
    [JsonPropertyName("OfferAccepted")]
    public required bool OfferAccepted { get; init; }

    /// <summary>The offer identifier.</summary>
    [JsonPropertyName("OfferId")]
    public required string OfferId { get; init; }

    /// <summary>The amount paid out for this item in major currency units.</summary>
    [JsonPropertyName("Payout")]
    public required decimal Payout { get; init; }

    /// <summary>The detected metal type, e.g. <c>Gold</c>.</summary>
    [JsonPropertyName("MetalType")]
    public required string MetalType { get; init; }

    /// <summary>The physical bag identifier, or empty when the item was returned.</summary>
    [JsonPropertyName("BagNumber")]
    public required string BagNumber { get; init; }

    /// <summary>The image-processing subfolder; placeholder empty string.</summary>
    [JsonPropertyName("ImageProcessingDir")]
    public required string ImageProcessingDir { get; init; }

    /// <summary>When the offer was made.</summary>
    [JsonPropertyName("OfferMadeOn")]
    public required DateTimeOffset? OfferMadeOn { get; init; }

    /// <summary>When the customer accepted or declined the offer.</summary>
    [JsonPropertyName("CustomerRespondedToOfferOn")]
    public required DateTimeOffset? CustomerRespondedToOfferOn { get; init; }
}
