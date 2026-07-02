using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Settlement;

namespace GoldKiosk.Contracts.V1.Events;

/// <summary>
/// SignalR <c>session_completed</c> payload — the terminal event carrying the receipt.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="Sequence">The per-session monotonic event sequence.</param>
/// <param name="State">The terminal session state (see <see cref="SessionStates"/>).</param>
/// <param name="Receipt">The receipt for the completed transaction.</param>
/// <param name="IsTest">Whether the session ran with one or more mocked devices.</param>
public sealed record SessionCompletedEvent(
    string SessionId,
    long Sequence,
    string State,
    ReceiptDto Receipt,
    bool IsTest);
