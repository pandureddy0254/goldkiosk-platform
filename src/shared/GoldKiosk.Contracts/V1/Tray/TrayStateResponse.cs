using GoldKiosk.Contracts.V1.Common;

namespace GoldKiosk.Contracts.V1.Tray;

/// <summary>
/// Response body for the tray open/close commands.
/// </summary>
/// <param name="SessionId">The session identifier.</param>
/// <param name="State">The session state after the command (see <see cref="SessionStates"/>).</param>
/// <param name="Tray">The tray movement status.</param>
/// <param name="Sequence">The per-session monotonic event sequence.</param>
public sealed record TrayStateResponse(string SessionId, string State, TrayDto Tray, long Sequence);
