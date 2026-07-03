using GoldKiosk.Contracts.V1.Events;

namespace GoldKiosk.Kiosk.Core.Orchestration;

/// <summary>
/// Port through which the session flow surfaces progress to the UI. Kiosk.Core defines the
/// abstraction; the Kiosk.Api host implements it over the SignalR hub. Every event carries
/// the per-session monotonic sequence issued by the session aggregate.
/// </summary>
public interface IKioskEventPublisher
{
    /// <summary>Publishes the <c>session_state_changed</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    /// <returns>A task that completes when the event has been dispatched.</returns>
    Task PublishSessionStateChangedAsync(SessionStateChangedEvent payload, CancellationToken cancellationToken = default);

    /// <summary>Publishes the <c>tray_state_changed</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    /// <returns>A task that completes when the event has been dispatched.</returns>
    Task PublishTrayStateChangedAsync(TrayStateChangedEvent payload, CancellationToken cancellationToken = default);

    /// <summary>Publishes the <c>analysis_progress</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    /// <returns>A task that completes when the event has been dispatched.</returns>
    Task PublishAnalysisProgressAsync(AnalysisProgressEvent payload, CancellationToken cancellationToken = default);

    /// <summary>Publishes the <c>identity_progress</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    /// <returns>A task that completes when the event has been dispatched.</returns>
    Task PublishIdentityProgressAsync(IdentityProgressEvent payload, CancellationToken cancellationToken = default);

    /// <summary>Publishes the <c>settlement_progress</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    /// <returns>A task that completes when the event has been dispatched.</returns>
    Task PublishSettlementProgressAsync(SettlementProgressEvent payload, CancellationToken cancellationToken = default);

    /// <summary>Publishes the <c>agent_status</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    /// <returns>A task that completes when the event has been dispatched.</returns>
    Task PublishAgentStatusAsync(AgentStatusEvent payload, CancellationToken cancellationToken = default);

    /// <summary>Publishes the <c>device_health_changed</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    /// <returns>A task that completes when the event has been dispatched.</returns>
    Task PublishDeviceHealthChangedAsync(DeviceHealthChangedEvent payload, CancellationToken cancellationToken = default);

    /// <summary>Publishes the terminal <c>session_completed</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    /// <returns>A task that completes when the event has been dispatched.</returns>
    Task PublishSessionCompletedAsync(SessionCompletedEvent payload, CancellationToken cancellationToken = default);

    /// <summary>Publishes the <c>session_aborted</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    /// <returns>A task that completes when the event has been dispatched.</returns>
    Task PublishSessionAbortedAsync(SessionAbortedEvent payload, CancellationToken cancellationToken = default);

    /// <summary>Publishes the <c>idle_warning</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <param name="cancellationToken">Cancels the publish.</param>
    /// <returns>A task that completes when the event has been dispatched.</returns>
    Task PublishIdleWarningAsync(IdleWarningEvent payload, CancellationToken cancellationToken = default);
}
