using GoldKiosk.Contracts.V1.Common;
using GoldKiosk.Contracts.V1.Identity;
using GoldKiosk.Contracts.V1.Offers;
using GoldKiosk.Contracts.V1.Payout;

namespace GoldKiosk.Contracts.V1.Sessions;

/// <summary>
/// Response body for <c>GET /api/v1/sessions/{id}</c> — the full state snapshot used for
/// UI crash/reload resume.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="State">The current session state (see <see cref="SessionStates"/>).</param>
/// <param name="Sequence">The per-session monotonic event sequence last emitted.</param>
/// <param name="Offer">The current offer, when one has been made.</param>
/// <param name="Identity">The identity-sequence progress, when identity has started.</param>
/// <param name="Payout">The payout status, when a payout method has been confirmed.</param>
/// <param name="IsTest">Whether the session ran with one or more mocked devices.</param>
public sealed record SessionSnapshotResponse(
    string SessionId,
    string State,
    long Sequence,
    OfferDto? Offer,
    IdentityProgressDto? Identity,
    PayoutStatusDto? Payout,
    bool IsTest);
