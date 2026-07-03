using GoldKiosk.Contracts.V1.Contact;
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
/// The combined contact-and-receipt capture — email, phone and channels in one call
/// (payload samples §7). Contact values are PII: stored, never logged.
/// </summary>
public static class ContactEndpoints
{
    /// <summary>Maps the contact endpoint onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapContactEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/sessions/{id}/contact", SubmitAsync)
            .AddEndpointFilter<IdempotencyFilter>()
            .AddEndpointFilter<ValidationFilter<ContactRequest>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Results<Accepted<StateResponse>, ProblemHttpResult>> SubmitAsync(
        string id,
        ContactRequest request,
        SessionRegistry sessions,
        TransactionOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (!sessions.TryGet(id, out TransactionSession? session))
        {
            return Problems.SessionNotFound(id);
        }

        Result result = await orchestrator.SubmitContactAsync(session, request, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Accepted((string?)null, new StateResponse(session.State))
            : Problems.FromDomainError(result.Error, session.Id);
    }
}
