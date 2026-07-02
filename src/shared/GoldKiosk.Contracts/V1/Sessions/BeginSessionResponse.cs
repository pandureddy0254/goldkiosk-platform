using GoldKiosk.Contracts.V1.Common;

namespace GoldKiosk.Contracts.V1.Sessions;

/// <summary>
/// Response body for <c>POST /api/v1/sessions</c>.
/// </summary>
/// <param name="SessionId">The new session identifier, e.g. <c>ses_01JZC…</c>.</param>
/// <param name="State">The session state (see <see cref="SessionStates"/>).</param>
/// <param name="Sequence">The per-session monotonic event sequence at creation.</param>
/// <param name="Features">The feature flags in effect for this session.</param>
/// <param name="OfferTtlSeconds">How long an offer stays locked once made, in seconds.</param>
/// <param name="IdleTimeoutSeconds">The idle timeout before an automatic abort, in seconds.</param>
/// <param name="TermsVersion">The current terms-and-conditions version the customer must accept.</param>
public sealed record BeginSessionResponse(
    string SessionId,
    string State,
    long Sequence,
    FeaturesDto Features,
    int OfferTtlSeconds,
    int IdleTimeoutSeconds,
    string TermsVersion);
