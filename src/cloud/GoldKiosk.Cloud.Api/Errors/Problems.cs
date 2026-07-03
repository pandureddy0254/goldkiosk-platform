using GoldKiosk.Contracts.V1.Cloud;
using GoldKiosk.Contracts.V1.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GoldKiosk.Cloud.Api.Errors;

/// <summary>
/// Factory for RFC 7807 responses with the stable <c>type</c> URIs from
/// <see cref="CloudProblemTypes"/> (Cloud API surface) and the shared
/// <see cref="ProblemTypes"/> codes. Detail strings never carry PII.
/// </summary>
internal static class Problems
{
    /// <summary>Builds the 401 for a failed kiosk login. Deliberately opaque.</summary>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult InvalidCredentials() =>
        TypedResults.Problem(
            type: CloudProblemTypes.AuthInvalidCredentials,
            title: "Invalid kiosk credentials",
            statusCode: StatusCodes.Status401Unauthorized,
            detail: "The kiosk code and PIN did not authenticate.");

    /// <summary>Builds the 503 for a rate read with no last-known rows.</summary>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult RatesUnavailable() =>
        TypedResults.Problem(
            type: CloudProblemTypes.RatesUnavailable,
            title: "Metal rates unavailable",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            detail: "No metal rates are available yet. Try again after the next rate sync.");

    /// <summary>Builds the 503 for an offer with no usable rate for the requested metal/karat.</summary>
    /// <param name="metal">The requested metal.</param>
    /// <param name="karat">The requested karat.</param>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult OfferRatesUnavailable(string metal, decimal karat) =>
        TypedResults.Problem(
            type: CloudProblemTypes.OfferRatesUnavailable,
            title: "Offer cannot be priced",
            statusCode: StatusCodes.Status503ServiceUnavailable,
            detail: "No usable market rate exists for the requested item.",
            extensions: new Dictionary<string, object?>
            {
                ["metal"] = metal,
                ["karat"] = karat,
            });

    /// <summary>Builds the 404 for an unknown live-agent review id.</summary>
    /// <param name="reviewId">The unknown review id.</param>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult ReviewNotFound(Guid reviewId) =>
        TypedResults.Problem(
            type: CloudProblemTypes.ReviewNotFound,
            title: "Item review not found",
            statusCode: StatusCodes.Status404NotFound,
            detail: "The item review id is unknown.",
            extensions: new Dictionary<string, object?> { ["review_id"] = reviewId });

    /// <summary>Builds the 404 for a kiosk principal with no fleet record.</summary>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult KioskNotFound() =>
        TypedResults.Problem(
            type: CloudProblemTypes.KioskNotFound,
            title: "Kiosk not found",
            statusCode: StatusCodes.Status404NotFound,
            detail: "The authenticated kiosk has no matching fleet record.");

    /// <summary>Builds the 404 for a tenant with no published terms document.</summary>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult TermsNotFound() =>
        TypedResults.Problem(
            type: CloudProblemTypes.TermsNotFound,
            title: "Terms not found",
            statusCode: StatusCodes.Status404NotFound,
            detail: "No terms & conditions document is published for this tenant.");

    /// <summary>Builds the 400 for a missing <c>Idempotency-Key</c> header.</summary>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult MissingIdempotencyKey() =>
        TypedResults.Problem(
            type: ProblemTypes.IdempotencyKeyConflict,
            title: "Idempotency-Key header is required",
            statusCode: StatusCodes.Status400BadRequest,
            detail: "Transaction-creating calls require an Idempotency-Key header.");

    /// <summary>Builds the 415 for an upload whose content type is not allow-listed.</summary>
    /// <param name="contentType">The rejected content type.</param>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult UnsupportedMediaType(string contentType) =>
        TypedResults.Problem(
            type: CloudProblemTypes.UnsupportedMediaType,
            title: "Unsupported image type",
            statusCode: StatusCodes.Status415UnsupportedMediaType,
            detail: "Only JPEG, PNG, BMP and WebP images are accepted.",
            extensions: new Dictionary<string, object?> { ["content_type"] = contentType });

    /// <summary>Builds the 413 for an upload exceeding the size limit.</summary>
    /// <param name="maxBytes">The configured limit in bytes.</param>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult UploadTooLarge(long maxBytes) =>
        TypedResults.Problem(
            type: CloudProblemTypes.UploadTooLarge,
            title: "Image too large",
            statusCode: StatusCodes.Status413PayloadTooLarge,
            detail: "An uploaded image exceeds the configured size limit.",
            extensions: new Dictionary<string, object?> { ["max_bytes"] = maxBytes });
}
