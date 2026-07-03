using GoldKiosk.Contracts.V1.Identity;
using GoldKiosk.Domain.Primitives;
using GoldKiosk.Kiosk.Api.Endpoints.Responses;
using GoldKiosk.Kiosk.Api.Errors;
using GoldKiosk.Kiosk.Api.Idempotency;
using GoldKiosk.Kiosk.Api.Orchestration;
using GoldKiosk.Kiosk.Api.Validation;
using GoldKiosk.Kiosk.Core.Sessions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GoldKiosk.Kiosk.Api.Endpoints;

/// <summary>
/// Identity endpoints (payload samples §6): start kicks the hardware-driven checklist;
/// signature clubs the drawn signature with the signed terms version.
/// </summary>
public static class IdentityEndpoints
{
    /// <summary>Maps the identity endpoints onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapIdentityEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/sessions/{id}/identity/start", StartAsync)
            .AddEndpointFilter<IdempotencyFilter>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/sessions/{id}/identity/signature", SignatureAsync)
            .AddEndpointFilter<IdempotencyFilter>()
            .AddEndpointFilter<ValidationFilter<IdentitySignatureRequest>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Results<Accepted<IdentityStartResponse>, ProblemHttpResult>> StartAsync(
        string id,
        SessionRegistry sessions,
        TransactionOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (!sessions.TryGet(id, out TransactionSession? session))
        {
            return Problems.SessionNotFound(id);
        }

        Result result = await orchestrator.StartIdentityAsync(session, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Accepted(
                (string?)null,
                new IdentityStartResponse(session.State, new IdentityCurrentStepDto("id_scan")))
            : Problems.FromDomainError(result.Error, session.Id);
    }

    private static async Task<Results<Accepted<StateResponse>, ProblemHttpResult>> SignatureAsync(
        string id,
        IdentitySignatureRequest request,
        SessionRegistry sessions,
        TransactionOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (!sessions.TryGet(id, out TransactionSession? session))
        {
            return Problems.SessionNotFound(id);
        }

        Result result = await orchestrator.SubmitSignatureAsync(session, request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Accepted((string?)null, new StateResponse(session.State))
            : Problems.FromDomainError(result.Error, session.Id);
    }
}
