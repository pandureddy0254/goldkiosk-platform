using GoldKiosk.Contracts.V1.Payout;
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
/// The combined payout call — method plus inline bank details (payload samples §8). Cash
/// validates the bill mix against the dispenser; infeasible amounts return 409
/// <c>payout.insufficient_cash</c> with the <c>available_methods</c> extension.
/// </summary>
public static class PayoutEndpoints
{
    /// <summary>Maps the payout endpoint onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapPayoutEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/sessions/{id}/payout", ConfirmAsync)
            .AddEndpointFilter<IdempotencyFilter>()
            .AddEndpointFilter<ValidationFilter<PayoutRequest>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Results<Accepted<PayoutConfirmedResponse>, ProblemHttpResult, ValidationProblem>>
        ConfirmAsync(
            string id,
            PayoutRequest request,
            SessionRegistry sessions,
            TransactionOrchestrator orchestrator,
            IOptions<FeaturesOptions> featuresOptions,
            CancellationToken cancellationToken)
    {
        if (!sessions.TryGet(id, out TransactionSession? session))
        {
            return Problems.SessionNotFound(id);
        }

        FeaturesOptions features = featuresOptions.Value;
        if (!features.PayoutMethods.Contains(request.Method, StringComparer.Ordinal))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["method"] = [$"Payout method '{request.Method}' is not enabled on this kiosk."],
            });
        }

        Result<PayoutStatusDto> result = await orchestrator.ConfirmPayoutAsync(
            session, request, cancellationToken);
        if (result.IsSuccess)
        {
            return TypedResults.Accepted(
                (string?)null, new PayoutConfirmedResponse(session.State, result.Value));
        }

        if (string.Equals(result.Error.Code, "payout.insufficient_cash", StringComparison.Ordinal))
        {
            List<string> available =
                [.. features.PayoutMethods.Where(m => !string.Equals(m, "cash", StringComparison.Ordinal))];
            return Problems.InsufficientCash(session.Id, available);
        }

        return Problems.FromDomainError(result.Error, session.Id);
    }
}
