using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Domain.Primitives;
using GoldKiosk.Kiosk.Api.Endpoints.Responses;
using GoldKiosk.Kiosk.Api.Errors;
using GoldKiosk.Kiosk.Api.Idempotency;
using GoldKiosk.Kiosk.Api.Orchestration;
using GoldKiosk.Kiosk.Api.Validation;
using GoldKiosk.Kiosk.Core.Pricing;
using GoldKiosk.Kiosk.Core.Sessions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GoldKiosk.Kiosk.Api.Endpoints;

/// <summary>
/// Offer actions: accept, decline, and the AI explainer sheet (payload samples §5).
/// </summary>
public static class OfferEndpoints
{
    /// <summary>Maps the offer endpoints onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapOfferEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/sessions/{id}/offer/accept", AcceptAsync)
            .AddEndpointFilter<IdempotencyFilter>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/sessions/{id}/offer/decline", DeclineAsync)
            .AddEndpointFilter<IdempotencyFilter>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/sessions/{id}/offer/explain", ExplainAsync)
            .AddEndpointFilter<ValidationFilter<ExplainOfferRequest>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Results<Accepted<StateSequenceResponse>, ProblemHttpResult>> AcceptAsync(
        string id,
        SessionRegistry sessions,
        TransactionOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (!sessions.TryGet(id, out TransactionSession? session))
        {
            return Problems.SessionNotFound(id);
        }

        Result result = await orchestrator.AcceptOfferAsync(session, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Accepted(
                (string?)null, new StateSequenceResponse(session.State, session.Sequence))
            : Problems.FromDomainError(result.Error, session.Id);
    }

    private static async Task<Results<Accepted<StateSequenceResponse>, ProblemHttpResult>> DeclineAsync(
        string id,
        SessionRegistry sessions,
        TransactionOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (!sessions.TryGet(id, out TransactionSession? session))
        {
            return Problems.SessionNotFound(id);
        }

        Result result = await orchestrator.DeclineOfferAsync(session, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Accepted(
                (string?)null, new StateSequenceResponse(session.State, session.Sequence))
            : Problems.FromDomainError(result.Error, session.Id);
    }

    private static async Task<Results<Ok<ExplainOfferResponse>, ProblemHttpResult>> ExplainAsync(
        string id,
        ExplainOfferRequest request,
        SessionRegistry sessions,
        IOfferExplainer explainer,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!sessions.TryGet(id, out TransactionSession? session))
        {
            return Problems.SessionNotFound(id);
        }

        session.Touch(timeProvider.GetUtcNow());
        if (session.Offer is null)
        {
            return Problems.FromDomainError(
                SessionErrors.InvalidState("explain the offer", session.State), session.Id);
        }

        ExplainOfferResponse response = await explainer.ExplainAsync(
            session.Offer, request.Question, cancellationToken);
        return TypedResults.Ok(response);
    }
}
