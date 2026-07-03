using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Core.Options;

/// <summary>
/// Options for the <c>MockRates</c> configuration section — the deterministic per-gram
/// rate table used by <see cref="Pricing.MockOfferCalculator"/> and the attract-loop rates
/// ticker until live cloud pricing arrives with Cloud.Api.
/// </summary>
public sealed class MockRatesOptions
{
    /// <summary>The configuration section name this options class binds from.</summary>
    public const string SectionName = "MockRates";

    /// <summary>The pure-gold rate per gram in major currency units.</summary>
    [Range(0.01, 1_000_000.0)]
    public decimal GoldPerGram { get; set; } = 108.94m;

    /// <summary>The pure-silver rate per gram in major currency units.</summary>
    [Range(0.01, 1_000_000.0)]
    public decimal SilverPerGram { get; set; } = 1.32m;

    /// <summary>The ISO 4217 currency the rates are quoted in.</summary>
    [Required(AllowEmptyStrings = false)]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "MockRates:Currency must be a three-letter ISO 4217 code.")]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// The store margin in percent of melt value paid to the customer (legacy default 70).
    /// </summary>
    [Range(1.0, 100.0)]
    public decimal StoreMarginPercent { get; set; } = 70.0m;

    /// <summary>Display-only day change for gold on the attract ticker, in percent.</summary>
    [Range(-100.0, 100.0)]
    public decimal GoldChangePercent { get; set; } = 0.42m;

    /// <summary>Display-only day change for silver on the attract ticker, in percent.</summary>
    [Range(-100.0, 100.0)]
    public decimal SilverChangePercent { get; set; } = -0.11m;
}
