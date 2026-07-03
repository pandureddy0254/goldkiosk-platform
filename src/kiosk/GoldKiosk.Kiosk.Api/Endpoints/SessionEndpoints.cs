using GoldKiosk.Contracts.V1.Identity;
using GoldKiosk.Contracts.V1.Payout;
using GoldKiosk.Contracts.V1.Sessions;
using GoldKiosk.Domain.Primitives;
using GoldKiosk.Kiosk.Api.Endpoints.Responses;
using GoldKiosk.Kiosk.Api.Errors;
using GoldKiosk.Kiosk.Api.Idempotency;
using GoldKiosk.Kiosk.Api.Orchestration;
using GoldKiosk.Kiosk.Api.Validation;
using GoldKiosk.Kiosk.Core.Options;
using GoldKiosk.Kiosk.Core.Sessions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace GoldKiosk.Kiosk.Api.Endpoints;

/// <summary>
/// Session lifecycle endpoints: begin, snapshot (crash resume), abort
/// (payload samples §1, §10, §14).
/// </summary>
public static class SessionEndpoints
{
    /// <summary>Maps the session endpoints onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapSessionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/sessions", BeginSession)
            .AddEndpointFilter<ValidationFilter<BeginSessionRequest>>()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        group.MapGet("/sessions/{id}", GetSnapshot)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/sessions/{id}/abort", AbortAsync)
            .AddEndpointFilter<IdempotencyFilter>()
            .AddEndpointFilter<ValidationFilter<AbortSessionRequest>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return group;
    }

    private static Results<Created<BeginSessionResponse>, ProblemHttpResult> BeginSession(
        BeginSessionRequest request,
        TransactionOrchestrator orchestrator,
        IOptions<FeaturesOptions> featuresOptions,
        IOptions<KioskOptions> kioskOptions)
    {
        Result<TransactionSession> result = orchestrator.BeginSession();
        if (result.IsFailure)
        {
            // Another non-terminal session is active: one customer at a time (409).
            return Problems.FromDomainError(result.Error);
        }

        TransactionSession session = result.Value;
        FeaturesOptions features = featuresOptions.Value;
        KioskOptions kiosk = kioskOptions.Value;

        var response = new BeginSessionResponse(
            session.Id,
            session.State,
            session.Sequence,
            new FeaturesDto(
                features.PawnEnabled,
                features.CryptoEnabled,
                features.FingerprintRequired,
                [.. features.PayoutMethods]),
            kiosk.OfferTtlSeconds,
            kiosk.IdleTimeoutSeconds,
            kiosk.TermsVersion);

        return TypedResults.Created($"/api/v1/sessions/{session.Id}", response);
    }

    private static Results<Ok<SessionSnapshotResponse>, ProblemHttpResult> GetSnapshot(
        string id,
        SessionRegistry sessions,
        TimeProvider timeProvider)
    {
        if (!sessions.TryGet(id, out TransactionSession? session))
        {
            return Problems.SessionNotFound(id);
        }

        session.Touch(timeProvider.GetUtcNow());

        IdentityProgressDto? identity = session.IdentitySteps.Count > 0
            ? new IdentityProgressDto(session.CurrentIdentityStep ?? string.Empty, [.. session.IdentitySteps])
            : null;
        PayoutStatusDto? payout = session.Payout is null
            ? null
            : new PayoutStatusDto(session.Payout.Method, session.Payout.BillMixOk);

        return TypedResults.Ok(new SessionSnapshotResponse(
            session.Id,
            session.State,
            session.Sequence,
            session.Offer,
            identity,
            payout,
            session.IsTest));
    }

    private static async Task<Results<Accepted<StateResponse>, ProblemHttpResult>> AbortAsync(
        string id,
        AbortSessionRequest request,
        SessionRegistry sessions,
        TransactionOrchestrator orchestrator,
        CancellationToken cancellationToken)
    {
        if (!sessions.TryGet(id, out TransactionSession? session))
        {
            return Problems.SessionNotFound(id);
        }

        Result result = await orchestrator.AbortAsync(
            session, request.Reason, request.ReturnItem, cancellationToken);
        return result.IsSuccess
            ? TypedResults.Accepted((string?)null, new StateResponse(session.State))
            : Problems.FromDomainError(result.Error, session.Id);
    }
}
