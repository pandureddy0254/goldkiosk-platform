using GoldKiosk.Contracts.V1.Agent;
using GoldKiosk.Domain.Primitives;
using GoldKiosk.Kiosk.Api.Errors;
using GoldKiosk.Kiosk.Api.Idempotency;
using GoldKiosk.Kiosk.Api.Orchestration;
using GoldKiosk.Kiosk.Api.Validation;
using GoldKiosk.Kiosk.Core.Sessions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GoldKiosk.Kiosk.Api.Endpoints;

/// <summary>
/// Live-agent escalation (payload samples §11): the mock connects then approves after a
/// short delay over SignalR. The real path (Cloud.Api review) never fabricates approval.
/// </summary>
public static class AgentEndpoints
{
    /// <summary>Maps the agent endpoint onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapAgentEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/sessions/{id}/agent/item-check", ItemCheckAsync)
            .AddEndpointFilter<IdempotencyFilter>()
            .AddEndpointFilter<ValidationFilter<AgentItemCheckRequest>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Results<Accepted<AgentStatusDto>, ProblemHttpResult>> ItemCheckAsync(
        string id,
        AgentItemCheckRequest request,
        SessionRegistry sessions,
        TransactionOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (!sessions.TryGet(id, out TransactionSession? session))
        {
            return Problems.SessionNotFound(id);
        }

        Result<AgentStatusDto> result = await orchestrator.RequestAgentItemCheckAsync(
            session, request.Trigger, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Accepted((string?)null, result.Value)
            : Problems.FromDomainError(result.Error, session.Id);
    }
}
