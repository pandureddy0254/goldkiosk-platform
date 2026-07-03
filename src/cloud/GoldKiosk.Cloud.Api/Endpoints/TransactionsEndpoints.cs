using GoldKiosk.Cloud.Api.Auth;
using GoldKiosk.Cloud.Api.Errors;
using GoldKiosk.Cloud.Api.Services.Transactions;
using GoldKiosk.Cloud.Api.Validation;
using GoldKiosk.Contracts.V1.Cloud.Transactions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GoldKiosk.Cloud.Api.Endpoints;

/// <summary>
/// Completed-transaction recording (replaces legacy <c>MAKE-SELL</c>). Transaction-creating
/// route: the <c>Idempotency-Key</c> header is mandatory and the kiosk's transaction id is
/// the durable dedupe anchor — replays return the original record.
/// </summary>
public static class TransactionsEndpoints
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    /// <summary>Maps the transactions endpoint onto the versioned group.</summary>
    /// <param name="group">The <c>/api/v1</c> route group.</param>
    /// <returns>The same group for chaining.</returns>
    public static RouteGroupBuilder MapTransactionsEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/transactions", RecordAsync)
            .RequireAuthorization(AuthorizationPolicies.Kiosk)
            .AddEndpointFilter<ValidationFilter<RecordTransactionRequest>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return group;
    }

    private static async Task<Results<Ok<RecordTransactionResponse>, ProblemHttpResult>> RecordAsync(
        RecordTransactionRequest request,
        HttpContext httpContext,
        ITransactionRecorder recorder,
        CurrentKioskService kiosk,
        CancellationToken cancellationToken)
    {
        string idempotencyKey = httpContext.Request.Headers[IdempotencyKeyHeader].ToString();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Problems.MissingIdempotencyKey();
        }

        if (kiosk.KioskId is not Guid kioskId || kiosk.TenantId is not Guid tenantId)
        {
            return Problems.KioskNotFound();
        }

        RecordTransactionResponse response =
            await recorder.RecordAsync(request, kioskId, tenantId, cancellationToken);
        return TypedResults.Ok(response);
    }
}
