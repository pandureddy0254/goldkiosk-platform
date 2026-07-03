using GoldKiosk.Contracts.V1.Identity;
using GoldKiosk.Contracts.V1.Payout;

namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// The common shape of session-command responses (offer accept/decline, identity start,
/// signature, contact, payout, settle, abort): the post-command state plus whichever
/// section the command produced. Fields absent on the wire stay <see langword="null"/>.
/// </summary>
/// <param name="State">The session state after the command, when returned.</param>
/// <param name="Sequence">The per-session monotonic event sequence, when returned.</param>
/// <param name="Identity">The identity-sequence progress, for identity commands.</param>
/// <param name="Payout">The confirmed payout status, for the payout command.</param>
public sealed record SessionActionResponse(
    string? State,
    long? Sequence,
    IdentityProgressDto? Identity,
    PayoutStatusDto? Payout);
