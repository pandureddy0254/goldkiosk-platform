using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.Api.Options;

/// <summary>
/// Region/market configuration for this deployment (adapted from platform2). The single
/// <see cref="DefaultCurrency"/> knob drives the whole currency-sensitive path — live-rate
/// fetch, rate storage, offer computation, transaction records — so nothing hardcodes INR.
/// Global-phase seam: resolve per tenant from <c>tenancy.tenant_configs.default_currency_code</c>
/// once one deployment serves multiple currencies.
/// </summary>
public sealed class RegionOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Region";

    /// <summary>Gets the ISO 4217 code this deployment operates in (e.g. <c>INR</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(3, MinimumLength = 3)]
    public string DefaultCurrency { get; init; } = "INR";
}
