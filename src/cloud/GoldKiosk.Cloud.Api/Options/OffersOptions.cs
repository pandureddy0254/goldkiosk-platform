using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.Api.Options;

/// <summary>
/// Offer computation policy. The margin lives in configuration (not code) per the
/// platform2 reuse map — the hardcoded 0.92 payout factor and 300 s lock were explicitly
/// flagged for externalization. A tenant-level margin overrides this default once the
/// tenant pricing config entity exists (TODO GK-TEN-1).
/// </summary>
public sealed class OffersOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Offers";

    /// <summary>Gets the default payout margin in percent of melt value (e.g. 92 = 92%).</summary>
    [Range(1.0, 100.0)]
    public decimal DefaultMarginPercent { get; init; } = 92.0m;

    /// <summary>Gets the house-favorable floor rounding step in major currency units.</summary>
    [Range(0.01, 1000.0)]
    public decimal RoundingStep { get; init; } = 5.0m;

    /// <summary>Gets the offer price-lock duration in seconds.</summary>
    [Range(30, 3600)]
    public int LockSeconds { get; init; } = 300;
}
