using GoldKiosk.Cloud.Api.Auth;
using GoldKiosk.Cloud.Api.Errors;
using GoldKiosk.Cloud.Api.Services.LiveAgent;
using GoldKiosk.Cloud.Api.Validation;
using GoldKiosk.Contracts.V1.Cloud.LiveAgent;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace GoldKiosk.Cloud.Api.Endpoints;

/// <summary>
/// Live-agent escalation (fail-closed): the kiosk uploads the tray image and polls for
/// the agent verdict; TTL expiry ends as <c>timed_out</c> and the item returns — an
/// auto-approve path does not exist. The agent verdict endpoint lives on the AdminPortal
/// surface, never here.
/// </summary>
public static class LiveAgentEndpoints
{
    private static readonly string[] AllowedContentTypes =
        ["image/jpeg", "image/png", "image/bmp", "image/webp"];

    /// <summary>Maps the live-agent endpoints onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapLiveAgentEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/live-agent/item-review", CreateReviewAsync)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .DisableAntiforgery() // bearer-authenticated machine API; no cookies in play
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesValidationProblem();

        group.MapGet("/live-agent/item-review/{id:guid}", GetReviewStatus)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/live-agent/availability", GetAvailability)
            .RequireAuthorization(AuthorizationPolicies.Kiosk);

        group.MapPost("/live-agent/availability", SetAvailability)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .AddEndpointFilter<ValidationFilter<SetAgentAvailabilityRequest>>()
            .ProducesValidationProblem();

        return group;
    }

    private static Results<Created<ItemReviewCreatedResponse>, ProblemHttpResult, ValidationProblem>
        CreateReviewAsync(
            [FromForm(Name = "transaction_ref")] string? transactionRef,
            IFormFile? image,
            ItemReviewStore reviews,
            CurrentKioskService kiosk)
    {
        if (string.IsNullOrWhiteSpace(transactionRef))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["transaction_ref"] = ["The transaction_ref form field is required."],
            });
        }

        if (image is null || image.Length == 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["image"] = ["A tray image is required."],
            });
        }

        if (!AllowedContentTypes.Contains(image.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return Problems.UnsupportedMediaType(image.ContentType);
        }

        if (kiosk.KioskId is not Guid kioskId || kiosk.TenantId is not Guid tenantId)
        {
            return Problems.KioskNotFound();
        }

        // TODO(GK-LA-1): persist the tray image to the restricted Blob container and the
        // review to a durable table, then notify agents over SignalR. Until then the
        // in-memory review keeps the polling contract (and its fail-closed timeout) intact.
        ItemReview review = reviews.Create(kioskId, tenantId, transactionRef.Trim());
        return TypedResults.Created(
            $"/api/v1/live-agent/item-review/{review.ReviewId}",
            new ItemReviewCreatedResponse(review.ReviewId, review.Status, review.ExpiresAt));
    }

    private static Results<Ok<ItemReviewStatusResponse>, ProblemHttpResult> GetReviewStatus(
        Guid id,
        ItemReviewStore reviews)
    {
        ItemReview? review = reviews.Get(id);
        return review is null
            ? Problems.ReviewNotFound(id)
            : TypedResults.Ok(new ItemReviewStatusResponse(review.ReviewId, review.Status, review.Reason));
    }

    private static Ok<AgentAvailabilityDto> GetAvailability(AgentAvailabilityStore availability) =>
        TypedResults.Ok(availability.Get());

    private static Ok<AgentAvailabilityDto> SetAvailability(
        SetAgentAvailabilityRequest request,
        AgentAvailabilityStore availability) =>
        TypedResults.Ok(availability.Set(request.IsAvailable, request.AgentsOnline));
}
