using GoldKiosk.Contracts.V1.Tray;
using GoldKiosk.Domain.Primitives;
using GoldKiosk.Kiosk.Api.Errors;
using GoldKiosk.Kiosk.Api.Idempotency;
using GoldKiosk.Kiosk.Api.Orchestration;
using GoldKiosk.Kiosk.Api.Validation;
using GoldKiosk.Kiosk.Core.Sessions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GoldKiosk.Kiosk.Api.Endpoints;

/// <summary>
/// The clubbed tray commands (payload samples §2–§3): open ships every pre-tray selection;
/// close ships the has-item flag plus batched client telemetry. No per-selection calls exist.
/// </summary>
public static class TrayEndpoints
{
    /// <summary>Maps the tray endpoints onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapTrayEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/sessions/{id}/tray/open", OpenAsync)
            .AddEndpointFilter<IdempotencyFilter>()
            .AddEndpointFilter<ValidationFilter<TrayOpenRequest>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapPost("/sessions/{id}/tray/close", CloseAsync)
            .AddEndpointFilter<IdempotencyFilter>()
            .AddEndpointFilter<ValidationFilter<TrayCloseRequest>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Results<Accepted<TrayStateResponse>, ProblemHttpResult>> OpenAsync(
        string id,
        TrayOpenRequest request,
        SessionRegistry sessions,
        TransactionOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (!sessions.TryGet(id, out TransactionSession? session))
        {
            return Problems.SessionNotFound(id);
        }

        Result result = await orchestrator.OpenTrayAsync(session, request.Setup, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Accepted(
                (string?)null,
                new TrayStateResponse(session.Id, session.State, new TrayDto("opening"), session.Sequence))
            : Problems.FromDomainError(result.Error, session.Id);
    }

    private static async Task<Results<Accepted<TrayStateResponse>, ProblemHttpResult>> CloseAsync(
        string id,
        TrayCloseRequest request,
        SessionRegistry sessions,
        TransactionOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (!sessions.TryGet(id, out TransactionSession? session))
        {
            return Problems.SessionNotFound(id);
        }

        Result result = await orchestrator.CloseTrayAsync(session, request.HasItem, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Accepted(
                (string?)null,
                new TrayStateResponse(session.Id, session.State, new TrayDto("closing"), session.Sequence))
            : Problems.FromDomainError(result.Error, session.Id);
    }
}
