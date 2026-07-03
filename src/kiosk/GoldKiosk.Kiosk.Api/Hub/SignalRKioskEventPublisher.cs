using GoldKiosk.Contracts.V1.Events;
using GoldKiosk.Contracts.V1.Hub;
using GoldKiosk.Kiosk.Core.Orchestration;
using Microsoft.AspNetCore.SignalR;

namespace GoldKiosk.Kiosk.Api.Hub;

/// <summary>
/// SignalR-backed <see cref="IKioskEventPublisher"/>: broadcasts every kiosk event to all
/// connected clients (a single-kiosk machine has exactly one UI plus optional diagnostics
/// listeners). Cancellation is honored per SignalR send.
/// </summary>
/// <param name="hubContext">The strongly-typed hub context for <see cref="KioskHub"/>.</param>
public sealed class SignalRKioskEventPublisher(IHubContext<KioskHub, IKioskHubClient> hubContext)
    : IKioskEventPublisher
{
    /// <inheritdoc />
    public Task PublishSessionStateChangedAsync(
        SessionStateChangedEvent payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return hubContext.Clients.All.SessionStateChanged(payload);
    }

    /// <inheritdoc />
    public Task PublishTrayStateChangedAsync(
        TrayStateChangedEvent payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return hubContext.Clients.All.TrayStateChanged(payload);
    }

    /// <inheritdoc />
    public Task PublishAnalysisProgressAsync(
        AnalysisProgressEvent payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return hubContext.Clients.All.AnalysisProgress(payload);
    }

    /// <inheritdoc />
    public Task PublishIdentityProgressAsync(
        IdentityProgressEvent payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return hubContext.Clients.All.IdentityProgress(payload);
    }

    /// <inheritdoc />
    public Task PublishSettlementProgressAsync(
        SettlementProgressEvent payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return hubContext.Clients.All.SettlementProgress(payload);
    }

    /// <inheritdoc />
    public Task PublishAgentStatusAsync(
        AgentStatusEvent payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return hubContext.Clients.All.AgentStatus(payload);
    }

    /// <inheritdoc />
    public Task PublishDeviceHealthChangedAsync(
        DeviceHealthChangedEvent payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return hubContext.Clients.All.DeviceHealthChanged(payload);
    }

    /// <inheritdoc />
    public Task PublishSessionCompletedAsync(
        SessionCompletedEvent payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return hubContext.Clients.All.SessionCompleted(payload);
    }

    /// <inheritdoc />
    public Task PublishSessionAbortedAsync(
        SessionAbortedEvent payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return hubContext.Clients.All.SessionAborted(payload);
    }

    /// <inheritdoc />
    public Task PublishIdleWarningAsync(
        IdleWarningEvent payload, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return hubContext.Clients.All.IdleWarning(payload);
    }
}
