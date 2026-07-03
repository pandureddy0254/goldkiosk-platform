namespace GoldKiosk.Contracts.V1.Cloud;

/// <summary>
/// Stable RFC 7807 ProblemDetails <c>type</c> URIs for the Cloud API (kiosk-facing
/// surface). Values are additive within a contract version; never repurpose a code.
/// Kiosk-edge codes live in <see cref="GoldKiosk.Contracts.V1.Common.ProblemTypes"/>.
/// </summary>
public static class CloudProblemTypes
{
    private const string Base = "https://goldkiosk.dev/problems/";

    /// <summary>The kiosk code + PIN pair did not authenticate.</summary>
    public const string AuthInvalidCredentials = Base + "auth.invalid_credentials";

    /// <summary>No metal rates are available (no live fetch and no last-known DB rates).</summary>
    public const string RatesUnavailable = Base + "rates.unavailable";

    /// <summary>An offer could not be computed because no usable rate exists.</summary>
    public const string OfferRatesUnavailable = Base + "offer.rates_unavailable";

    /// <summary>The referenced offer quote is unknown or has expired.</summary>
    public const string OfferQuoteNotFound = Base + "offer.quote_not_found";

    /// <summary>The live-agent item review id is unknown.</summary>
    public const string ReviewNotFound = Base + "review.not_found";

    /// <summary>The authenticated kiosk has no matching fleet record.</summary>
    public const string KioskNotFound = Base + "kiosk.not_found";

    /// <summary>No terms &amp; conditions document is published for the tenant.</summary>
    public const string TermsNotFound = Base + "content.terms_not_found";

    /// <summary>The uploaded file's content type is not on the allow-list.</summary>
    public const string UnsupportedMediaType = Base + "upload.unsupported_media_type";

    /// <summary>The uploaded file exceeds the configured size limit.</summary>
    public const string UploadTooLarge = Base + "upload.too_large";
}
