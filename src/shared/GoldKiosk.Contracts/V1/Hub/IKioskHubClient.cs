using GoldKiosk.Contracts.V1.Events;

namespace GoldKiosk.Contracts.V1.Hub;

/// <summary>
/// Strongly-typed SignalR client contract for the <c>/hubs/kiosk</c> hub
/// (server → client only; REST remains the source of truth). Method names are the
/// PascalCase forms of the snake_case wire event names.
/// </summary>
public interface IKioskHubClient
{
    /// <summary>Delivers the <c>session_state_changed</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <returns>A task that completes when the client has handled the event.</returns>
    Task SessionStateChanged(SessionStateChangedEvent payload);

    /// <summary>Delivers the <c>tray_state_changed</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <returns>A task that completes when the client has handled the event.</returns>
    Task TrayStateChanged(TrayStateChangedEvent payload);

    /// <summary>Delivers the <c>analysis_progress</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <returns>A task that completes when the client has handled the event.</returns>
    Task AnalysisProgress(AnalysisProgressEvent payload);

    /// <summary>Delivers the <c>identity_progress</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <returns>A task that completes when the client has handled the event.</returns>
    Task IdentityProgress(IdentityProgressEvent payload);

    /// <summary>Delivers the <c>settlement_progress</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <returns>A task that completes when the client has handled the event.</returns>
    Task SettlementProgress(SettlementProgressEvent payload);

    /// <summary>Delivers the <c>agent_status</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <returns>A task that completes when the client has handled the event.</returns>
    Task AgentStatus(AgentStatusEvent payload);

    /// <summary>Delivers the <c>device_health_changed</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <returns>A task that completes when the client has handled the event.</returns>
    Task DeviceHealthChanged(DeviceHealthChangedEvent payload);

    /// <summary>Delivers the <c>session_completed</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <returns>A task that completes when the client has handled the event.</returns>
    Task SessionCompleted(SessionCompletedEvent payload);

    /// <summary>Delivers the <c>session_aborted</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <returns>A task that completes when the client has handled the event.</returns>
    Task SessionAborted(SessionAbortedEvent payload);

    /// <summary>Delivers the <c>idle_warning</c> event.</summary>
    /// <param name="payload">The event payload.</param>
    /// <returns>A task that completes when the client has handled the event.</returns>
    Task IdleWarning(IdleWarningEvent payload);
}
