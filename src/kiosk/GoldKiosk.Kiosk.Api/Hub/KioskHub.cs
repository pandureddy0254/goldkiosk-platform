using GoldKiosk.Contracts.V1.Hub;
using Microsoft.AspNetCore.SignalR;

namespace GoldKiosk.Kiosk.Api.Hub;

/// <summary>
/// The kiosk SignalR hub at <c>/hubs/kiosk</c>. Server → client only: the UI listens for
/// session/tray/analysis/identity/settlement events; REST remains the source of truth.
/// Localhost-only single-kiosk machine — no client-invokable methods, no groups.
/// </summary>
public sealed class KioskHub : Hub<IKioskHubClient>
{
}
