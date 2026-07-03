namespace GoldKiosk.Kiosk.Api.Idempotency;

/// <summary>
/// In-memory replay cache for idempotent session commands: the original successful result
/// is returned for a repeated <c>Idempotency-Key</c>. Which keys were processed also lives
/// in the session journal (crash-durable); this cache holds the response bodies for the
/// process lifetime, capped to the most recent entries.
/// </summary>
public sealed class IdempotencyStore
{
    // Size cap: a single-customer kiosk issues a handful of keyed commands per session,
    // so 200 entries comfortably covers the replay window of any active session while
    // bounding memory over a long-running process. Oldest entries are evicted first; an
    // evicted key still conflicts (409) via the session journal rather than replaying.
    private const int MaxEntries = 200;

    private readonly Lock _gate = new();
    private readonly Dictionary<string, IResult> _responses = new(StringComparer.Ordinal);
    private readonly Queue<string> _insertionOrder = new();

    /// <summary>Looks up the cached response for a processed key.</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="key">The idempotency key.</param>
    /// <returns>The original result, or <see langword="null"/> when not cached.</returns>
    public IResult? GetResponse(string sessionId, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (_gate)
        {
            return _responses.GetValueOrDefault(Compose(sessionId, key));
        }
    }

    /// <summary>Caches the successful response for a processed key.</summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="key">The idempotency key.</param>
    /// <param name="response">The result to replay.</param>
    public void CacheResponse(string sessionId, string key, IResult response)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(response);
        string composed = Compose(sessionId, key);
        lock (_gate)
        {
            if (_responses.TryAdd(composed, response))
            {
                _insertionOrder.Enqueue(composed);
            }
            else
            {
                _responses[composed] = response;
            }

            while (_insertionOrder.Count > MaxEntries)
            {
                _responses.Remove(_insertionOrder.Dequeue());
            }
        }
    }

    private static string Compose(string sessionId, string key) => sessionId + "|" + key;
}
