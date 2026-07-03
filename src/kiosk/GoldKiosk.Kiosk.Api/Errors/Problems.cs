using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Domain.Primitives;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GoldKiosk.Kiosk.Api.Errors;

/// <summary>
/// Factory for RFC 7807 responses with the stable <c>type</c> URIs from
/// <see cref="ProblemTypes"/> (payload samples §15). Domain-error codes are the URI
/// suffixes, so Core failures map mechanically onto the wire.
/// </summary>
internal static class Problems
{
    private const string BaseUri = "https://goldkiosk.dev/problems/";

    /// <summary>Builds the 404 for an unknown session id.</summary>
    /// <param name="sessionId">The unknown session id (an identifier, not PII).</param>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult SessionNotFound(string sessionId) =>
        TypedResults.Problem(
            type: ProblemTypes.SessionNotFound,
            title: "Session not found",
            statusCode: StatusCodes.Status404NotFound,
            detail: "The session id is unknown to this kiosk.",
            extensions: new Dictionary<string, object?> { ["session_id"] = sessionId });

    /// <summary>Builds the 400 for a missing <c>Idempotency-Key</c> header.</summary>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult MissingIdempotencyKey() =>
        TypedResults.Problem(
            type: ProblemTypes.IdempotencyKeyConflict,
            title: "Idempotency-Key header is required",
            statusCode: StatusCodes.Status400BadRequest,
            detail: "State-changing session calls require an Idempotency-Key header.");

    /// <summary>Builds the 409 for an idempotency key reused across operations.</summary>
    /// <param name="sessionId">The session id.</param>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult IdempotencyKeyConflict(string sessionId) =>
        TypedResults.Problem(
            type: ProblemTypes.IdempotencyKeyConflict,
            title: "Idempotency key conflict",
            statusCode: StatusCodes.Status409Conflict,
            detail: "This idempotency key was already used for a different operation.",
            extensions: new Dictionary<string, object?> { ["session_id"] = sessionId });

    /// <summary>
    /// Builds the 409 for an infeasible cash payout, carrying the <c>available_methods</c>
    /// extension so the UI can reroute (payload samples §8).
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="availableMethods">The payout methods still available.</param>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult InsufficientCash(string sessionId, IReadOnlyList<string> availableMethods) =>
        TypedResults.Problem(
            type: ProblemTypes.PayoutInsufficientCash,
            title: "Cash unavailable for this amount",
            statusCode: StatusCodes.Status409Conflict,
            detail: "Choose bank transfer or debit card, or return your item.",
            extensions: new Dictionary<string, object?>
            {
                ["session_id"] = sessionId,
                ["available_methods"] = availableMethods,
            });

    /// <summary>Builds the 404 for an unknown device key.</summary>
    /// <param name="deviceKey">The unknown device key.</param>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult DeviceNotFound(string deviceKey) =>
        TypedResults.Problem(
            type: ProblemTypes.DeviceUnavailable,
            title: "Device not found",
            statusCode: StatusCodes.Status404NotFound,
            detail: "No device is registered under this key.",
            extensions: new Dictionary<string, object?> { ["device_key"] = deviceKey });

    /// <summary>Maps a Core <see cref="DomainError"/> onto its wire problem (409).</summary>
    /// <param name="error">The domain error; its code is the type-URI suffix.</param>
    /// <param name="sessionId">The session id, when the failure concerns an existing session.</param>
    /// <returns>The problem result.</returns>
    public static ProblemHttpResult FromDomainError(DomainError error, string? sessionId = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        Dictionary<string, object?>? extensions = sessionId is null
            ? null
            : new Dictionary<string, object?> { ["session_id"] = sessionId };
        return TypedResults.Problem(
            type: BaseUri + error.Code,
            title: TitleFor(error.Code),
            statusCode: StatusCodes.Status409Conflict,
            detail: error.Message,
            extensions: extensions);
    }

    private static string TitleFor(string code) => code switch
    {
        "session.invalid_state" => "Action not valid in the current session state",
        "offer.expired" => "The offer has expired",
        "offer.already_actioned" => "The offer was already actioned",
        "payout.insufficient_cash" => "Cash unavailable for this amount",
        _ => "The request could not be processed",
    };
}
