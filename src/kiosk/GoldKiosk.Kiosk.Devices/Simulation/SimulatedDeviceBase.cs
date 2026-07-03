using System.Runtime.CompilerServices;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Exceptions;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>
/// Base plumbing for all simulated devices: health-state transitions with
/// <see cref="HealthChanged"/> notification, operation latency scaled by
/// <see cref="SimulationOptions.LatencyMultiplier"/>, and fault injection — a device listed
/// in <see cref="SimulationOptions.FaultDevices"/> connects successfully but its operations
/// throw <see cref="SimulatedDeviceFaultException"/> (ADR 0004 scripted failure mode).
/// Simulators are deterministic production code covered by the same contract tests as real
/// adapters. Time flows through the injected <see cref="System.TimeProvider"/> so tests can
/// use a fake.
/// </summary>
public abstract class SimulatedDeviceBase : IKioskDevice
{
    private const int ConnectLatencyMs = 150;
    private const int ProbeLatencyMs = 50;

    private readonly SimulationOptions _simulation;
    private DeviceHealth _health = new(DeviceState.NotInitialized);

    /// <summary>Initializes the simulated device plumbing.</summary>
    /// <param name="key">Canonical device key from <see cref="DeviceKeys"/>.</param>
    /// <param name="displayName">Human-readable device name.</param>
    /// <param name="isCritical">Whether the kiosk cannot trade without this device.</param>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays and probe timing.</param>
    protected SimulatedDeviceBase(
        string key,
        string displayName,
        bool isCritical,
        SimulationOptions simulation,
        TimeProvider timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(simulation);
        ArgumentNullException.ThrowIfNull(timeProvider);

        Key = key;
        DisplayName = displayName;
        IsCritical = isCritical;
        _simulation = simulation;
        TimeProvider = timeProvider;
    }

    /// <inheritdoc />
    public string Key { get; }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public bool IsCritical { get; }

    /// <inheritdoc />
    public DeviceMode Mode => DeviceMode.Mock;

    /// <inheritdoc />
    public DeviceHealth Health => _health;

    /// <inheritdoc />
    public event EventHandler<DeviceHealth>? HealthChanged;

    /// <summary>The injected time source used for delays and probe timing.</summary>
    protected TimeProvider TimeProvider { get; }

    /// <summary>Whether this device is configured for fault injection.</summary>
    protected bool IsFaultInjected =>
        _simulation.FaultDevices.Contains(Key, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        SetHealth(DeviceState.Connecting);
        await DelayAsync(ConnectLatencyMs, cancellationToken).ConfigureAwait(false);
        SetHealth(DeviceState.Ready);
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        SetHealth(DeviceState.Disconnected);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<DeviceProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
    {
        long started = TimeProvider.GetTimestamp();
        await DelayAsync(ProbeLatencyMs, cancellationToken).ConfigureAwait(false);
        long elapsedMs = (long)TimeProvider.GetElapsedTime(started).TotalMilliseconds;

        return IsFaultInjected
            ? new DeviceProbeResult(false, elapsedMs, $"Simulated fault injected for '{Key}'.")
            : new DeviceProbeResult(true, elapsedMs, "Simulated device healthy.");
    }

    /// <summary>Updates <see cref="Health"/> and raises <see cref="HealthChanged"/> when it changed.</summary>
    /// <param name="state">The new lifecycle state.</param>
    /// <param name="detail">Optional detail for the snapshot.</param>
    protected void SetHealth(DeviceState state, string? detail = null)
    {
        var updated = new DeviceHealth(state, detail);
        if (_health == updated)
        {
            return;
        }

        _health = updated;
        HealthChanged?.Invoke(this, updated);
    }

    /// <summary>Waits for the given base latency scaled by the configured multiplier.</summary>
    /// <param name="milliseconds">Base latency in milliseconds at multiplier 1.0.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>A task that completes after the scaled delay.</returns>
    protected Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(milliseconds);
        double scaled = milliseconds * Math.Max(0d, _simulation.LatencyMultiplier);
        return Task.Delay(TimeSpan.FromMilliseconds(scaled), TimeProvider, cancellationToken);
    }

    /// <summary>
    /// Call at the start of every operation: when this device is fault-injected, marks the
    /// device <see cref="DeviceState.Faulted"/> and throws.
    /// </summary>
    /// <param name="operation">The operation name (supplied by the compiler).</param>
    /// <exception cref="SimulatedDeviceFaultException">The device is listed in <c>FaultDevices</c>.</exception>
    protected void ThrowIfFaulted([CallerMemberName] string operation = "")
    {
        if (!IsFaultInjected)
        {
            return;
        }

        SetHealth(DeviceState.Faulted, $"Simulated fault during '{operation}'.");
        throw new SimulatedDeviceFaultException(Key, operation);
    }
}
