namespace GoldKiosk.Kiosk.Devices.Abstractions;

/// <summary>
/// Base contract every kiosk hardware port extends. Implementations come in matched pairs —
/// a real driver and a simulator — selected per device by <c>DevicesOptions.ResolveMode</c>
/// at composition time (ADR 0004). Only the Kiosk.Api host talks to devices; the UI consumes
/// health through REST + SignalR.
/// </summary>
/// <remarks>
/// Registration guidance (the host owns DI; this library has no framework references):
/// for each key in <see cref="DeviceKeys.All"/> the host constructs the simulated or real
/// implementation per the resolved <see cref="DeviceMode"/>, registers it as a singleton
/// against its port interface, and hands the full set to a
/// <c>Registry.DeviceRegistry</c> registered as the <see cref="IDeviceRegistry"/> singleton.
/// <see cref="HealthChanged"/> is raised synchronously on the calling thread; subscribers
/// must not block.
/// </remarks>
public interface IKioskDevice
{
    /// <summary>Canonical snake_case device key from <see cref="DeviceKeys"/>.</summary>
    string Key { get; }

    /// <summary>Human-readable name for dashboards and diagnostics reports.</summary>
    string DisplayName { get; }

    /// <summary>
    /// <see langword="true"/> when the kiosk cannot trade without this device
    /// (a faulted critical device locks new sessions; non-critical devices degrade a flow).
    /// </summary>
    bool IsCritical { get; }

    /// <summary>Whether this instance is the real driver or the simulator.</summary>
    DeviceMode Mode { get; }

    /// <summary>The most recent health snapshot.</summary>
    DeviceHealth Health { get; }

    /// <summary>Raised whenever <see cref="Health"/> changes.</summary>
    event EventHandler<DeviceHealth>? HealthChanged;

    /// <summary>Connects to / initializes the device.</summary>
    /// <param name="cancellationToken">Cancels the connection attempt.</param>
    /// <returns>A task that completes when the device is connected or faulted.</returns>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Disconnects from the device and releases its resources.</summary>
    /// <param name="cancellationToken">Cancels the disconnect.</param>
    /// <returns>A task that completes when the device is disconnected.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Runs a non-destructive diagnostic self-check of the device.</summary>
    /// <param name="cancellationToken">Cancels the probe.</param>
    /// <returns>The probe outcome including elapsed time.</returns>
    Task<DeviceProbeResult> ProbeAsync(CancellationToken cancellationToken = default);
}
