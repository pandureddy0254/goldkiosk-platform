using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace GoldKiosk.Kiosk.Core.Sessions;

/// <summary>
/// Thread-safe in-memory registry of this kiosk's sessions (a single-customer machine
/// normally holds one active session; terminal sessions stay resolvable for snapshot reads
/// until process restart). Durability lives in the file journal, not here (ADR 0002).
/// </summary>
public sealed class SessionRegistry
{
    private readonly ConcurrentDictionary<string, TransactionSession> _sessions =
        new(StringComparer.Ordinal);

    /// <summary>All non-terminal sessions, for the idle-timeout watchdog.</summary>
    public IReadOnlyList<TransactionSession> ActiveSessions =>
        [.. _sessions.Values.Where(s => !s.IsTerminal)];

    /// <summary>Registers a newly begun session.</summary>
    /// <param name="session">The session to register.</param>
    /// <exception cref="ArgumentException">A session with the same id is already registered.</exception>
    public void Add(TransactionSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!_sessions.TryAdd(session.Id, session))
        {
            throw new ArgumentException($"Session '{session.Id}' is already registered.", nameof(session));
        }
    }

    /// <summary>Looks up a session by id.</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="session">The session, when found.</param>
    /// <returns><see langword="true"/> when the session exists.</returns>
    public bool TryGet(string sessionId, [NotNullWhen(true)] out TransactionSession? session)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _sessions.TryGetValue(sessionId, out session);
    }
}
