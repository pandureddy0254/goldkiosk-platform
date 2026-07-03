using GoldKiosk.Cloud.Api.Auth;
using GoldKiosk.Cloud.Api.Errors;
using GoldKiosk.Cloud.Api.Services.Offers;
using GoldKiosk.Cloud.Api.Validation;
using GoldKiosk.Contracts.V1.Cloud.Offers;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GoldKiosk.Cloud.Api.Endpoints;

/// <summary>
/// Offer computation (replaces legacy <c>GET-OFFER</c>) and the grounded, templated
/// "How was this calculated?" explainer.
/// </summary>
public static class OffersEndpoints
{
    /// <summary>Maps the offer endpoints onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapOffersEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/offers", ComputeAsync)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .AddEndpointFilter<ValidationFilter<CloudOfferRequest>>()
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesValidationProblem();

        group.MapPost("/offers/explain", Explain)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .AddEndpointFilter<ValidationFilter<CloudExplainOfferRequest>>()
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Results<Ok<CloudOfferResponse>, ProblemHttpResult>> ComputeAsync(
        CloudOfferRequest request,
        IOfferService offers,
        CancellationToken cancellationToken)
    {
        CloudOfferResponse? response = await offers.ComputeOfferAsync(request, cancellationToken);
        return response is null
            ? Problems.OfferRatesUnavailable(request.Metal, request.Karat)
            : TypedResults.Ok(response);
    }

    private static Ok<CloudExplainOfferResponse> Explain(
        CloudExplainOfferRequest request,
        IOfferExplanationService explanations)
    {
        return TypedResults.Ok(explanations.Explain(request));
    }
}
