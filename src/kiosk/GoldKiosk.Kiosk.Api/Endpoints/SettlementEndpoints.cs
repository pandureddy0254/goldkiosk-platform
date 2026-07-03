using GoldKiosk.Domain.Primitives;
using GoldKiosk.Kiosk.Api.Endpoints.Responses;
using GoldKiosk.Kiosk.Api.Errors;
using GoldKiosk.Kiosk.Api.Idempotency;
using GoldKiosk.Kiosk.Api.Orchestration;
using GoldKiosk.Kiosk.Core.Sessions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GoldKiosk.Kiosk.Api.Endpoints;

/// <summary>
/// Settlement (payload samples §9): bag + dispense/transfer with progress over SignalR;
/// the terminal receipt arrives in <c>session_completed</c>. Idempotency-protected —
/// a replay never double-dispenses.
/// </summary>
public static class SettlementEndpoints
{
    /// <summary>Maps the settle endpoint onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapSettlementEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/sessions/{id}/settle", SettleAsync)
            .AddEndpointFilter<IdempotencyFilter>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<Results<Accepted<StateResponse>, ProblemHttpResult>> SettleAsync(
        string id,
        SessionRegistry sessions,
        TransactionOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (!sessions.TryGet(id, out TransactionSession? session))
        {
            return Problems.SessionNotFound(id);
        }

        Result result = await orchestrator.SettleAsync(session, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Accepted((string?)null, new StateResponse(session.State))
            : Problems.FromDomainError(result.Error, session.Id);
    }
}
