using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Offers;

namespace GoldKiosk.Contracts.V1.Events;

/// <summary>
/// SignalR <c>session_state_changed</c> payload — the session moved to a new state.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="Sequence">The per-session monotonic event sequence.</param>
/// <param name="State">The new session state (see <see cref="SessionStates"/>).</param>
/// <param name="Offer">The offer, when the new state is <c>offer</c>.</param>
/// <param name="Rejection">The rejection details, when the item was rejected.</param>
public sealed record SessionStateChangedEvent(
    string SessionId,
    long Sequence,
    string State,
    OfferDto? Offer,
    RejectionDto? Rejection);
