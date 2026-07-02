using System.Runtime.CompilerServices;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Exceptions;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Base for the not-yet-wired real drivers (Phase 4 of the mock-first build order,
/// design note §6). Constructing one touches no hardware; connecting reports
/// <see cref="DeviceState.Faulted"/> so the composed device set degrades visibly rather than
/// crashing, and every operation throws <see cref="DeviceNotWiredException"/> via
/// <see cref="NotWired"/>. Each concrete stub documents the target SDK from the legacy port
/// map so Phase 4 wiring is mechanical.
/// </summary>
public abstract class RealDeviceStub : IKioskDevice
{
    private DeviceHealth _health = new(DeviceState.NotInitialized);

    /// <summary>Initializes the stub's identity.</summary>
    /// <param name="key">Canonical device key from <see cref="DeviceKeys"/>.</param>
    /// <param name="displayName">Human-readable device name.</param>
    /// <param name="isCritical">Whether the kiosk cannot trade without this device.</param>
    protected RealDeviceStub(string key, string displayName, bool isCritical)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Key = key;
        DisplayName = displayName;
        IsCritical = isCritical;
    }

    /// <inheritdoc />
    public string Key { get; }

    /// <inheritdoc />
    public string DisplayName { get; }

    /// <inheritdoc />
    public bool IsCritical { get; }

    /// <inheritdoc />
    public DeviceMode Mode => DeviceMode.Real;

    /// <inheritdoc />
    public DeviceHealth Health => _health;

    /// <inheritdoc />
    public event EventHandler<DeviceHealth>? HealthChanged;

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        SetHealth(DeviceState.Connecting);
        SetHealth(DeviceState.Faulted, $"Real driver for '{Key}' is not wired yet (Phase 4).");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        SetHealth(DeviceState.Disconnected);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<DeviceProbeResult> ProbeAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new DeviceProbeResult(
            Passed: false,
            ElapsedMs: 0,
            Detail: $"Real driver for '{Key}' is not wired yet (Phase 4)."));

    /// <summary>Creates the exception every stubbed operation throws.</summary>
    /// <param name="operation">The operation name (supplied by the compiler).</param>
    /// <returns>The exception to throw.</returns>
    protected DeviceNotWiredException NotWired([CallerMemberName] string operation = "") =>
        new(Key, operation);

    private void SetHealth(DeviceState state, string? detail = null)
    {
        var updated = new DeviceHealth(state, detail);
        if (_health == updated)
        {
            return;
        }

        _health = updated;
        HealthChanged?.Invoke(this, updated);
    }
}
