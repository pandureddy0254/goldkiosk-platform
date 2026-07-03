using GoldKiosk.Kiosk.Api.Errors;
using GoldKiosk.Kiosk.Core.Sessions;

namespace GoldKiosk.Kiosk.Api.Idempotency;

/// <summary>
/// Endpoint filter enforcing the mandatory <c>Idempotency-Key</c> header on state-changing
/// session commands: a missing header is a 400; a replayed key returns the original
/// successful response; the same key on a different operation is a 409
/// <c>idempotency.key_conflict</c>. Keys are recorded on the session (and thereby in the
/// crash-durable journal).
/// </summary>
/// <param name="store">The replay cache.</param>
/// <param name="sessions">The session registry.</param>
public sealed class IdempotencyFilter(IdempotencyStore store, SessionRegistry sessions) : IEndpointFilter
{
    /// <summary>The required request header.</summary>
    public const string HeaderName = "Idempotency-Key";

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        HttpContext http = context.HttpContext;
        string key = http.Request.Headers[HeaderName].ToString();
        if (string.IsNullOrWhiteSpace(key))
        {
            return Problems.MissingIdempotencyKey();
        }

        if (http.Request.RouteValues["id"] is not string sessionId
            || !sessions.TryGet(sessionId, out TransactionSession? session))
        {
            // Unknown session: fall through so the handler produces the 404.
            return await next(context);
        }

        string operation = http.Request.Path.ToString();
        if (!session.TryRecordIdempotencyKey(key, operation, out string? existingOperation))
        {
            if (!string.Equals(existingOperation, operation, StringComparison.Ordinal))
            {
                return Problems.IdempotencyKeyConflict(sessionId);
            }

            IResult? cached = store.GetResponse(sessionId, key);
            // Recorded but not cached (e.g. the original attempt failed before completing):
            // surface the conflict rather than re-executing a possibly-settled command.
            // Concurrent in-flight duplicates land here too — the key is recorded before
            // the first call finishes, so the duplicate gets a 409 rather than a replay.
            // Deliberate: a keyed command is never executed twice.
            return cached ?? (object)Problems.IdempotencyKeyConflict(sessionId);
        }

        object? result = await next(context);

        // Results<T1, T2, ...> union types wrap the real result (INestedHttpResult);
        // unwrap before inspecting the status code so successes are cached for replay.
        IResult? inner = result switch
        {
            INestedHttpResult nested => nested.Result,
            IResult direct => direct,
            _ => null,
        };
        if (inner is IStatusCodeHttpResult { StatusCode: >= StatusCodes.Status200OK and < StatusCodes.Status300MultipleChoices })
        {
            store.CacheResponse(sessionId, key, inner);
        }

        return result;
    }
}
