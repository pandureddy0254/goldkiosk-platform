using System.Runtime.CompilerServices;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Exceptions;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Base plumbing for all real hardware drivers: health-state transitions with
/// <see cref="HealthChanged"/> notification and a fail-visible connect contract.
/// Constructors never touch hardware; <see cref="ConnectAsync"/> runs the driver's connect
/// sequence and converts any failure into <see cref="DeviceState.Faulted"/> health (with a
/// machine-readable detail such as <c>vendor_sdk_missing:*</c>) instead of throwing, so a
/// missing SDK or unplugged cable degrades the device set visibly rather than crashing the
/// host. Time flows through the injected <see cref="System.TimeProvider"/>.
/// </summary>
public abstract class RealDeviceBase : IKioskDevice, IDisposable
{
    private DeviceHealth _health = new(DeviceState.NotInitialized);

    /// <summary>Initializes the real-driver plumbing.</summary>
    /// <param name="key">Canonical device key from <see cref="DeviceKeys"/>.</param>
    /// <param name="displayName">Human-readable device name.</param>
    /// <param name="isCritical">Whether the kiosk cannot trade without this device.</param>
    /// <param name="timeProvider">Time source for waits and probe timing; <see langword="null"/> uses the system clock.</param>
    protected RealDeviceBase(string key, string displayName, bool isCritical, TimeProvider? timeProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        Key = key;
        DisplayName = displayName;
        IsCritical = isCritical;
        TimeProvider = timeProvider ?? TimeProvider.System;
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

    /// <summary>The injected time source used for delays and probe timing.</summary>
    protected TimeProvider TimeProvider { get; }

    /// <inheritdoc />
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        SetHealth(DeviceState.Connecting);
        try
        {
            await ConnectCoreAsync(cancellationToken).ConfigureAwait(false);
            SetHealth(DeviceState.Ready);
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Disconnected, "connect_cancelled");
            throw;
        }
        catch (DeviceConnectFailedException ex)
        {
            SetHealth(DeviceState.Faulted, ex.Message);
        }
        catch (Exception ex)
        {
            // Boundary handling by design (fail fast at boundaries, resilient across them):
            // any hardware/transport failure becomes Faulted health with a non-PII detail so
            // composition survives and Diagnostics can report the exact fault.
            SetHealth(DeviceState.Faulted, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await DisconnectCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Disconnect is best-effort teardown; a failing transport must not stop shutdown.
            // The failure is preserved on the final health snapshot below.
            SetHealth(DeviceState.Disconnected, $"disconnect_error:{ex.GetType().Name}");
            return;
        }

        SetHealth(DeviceState.Disconnected);
    }

    /// <inheritdoc />
    public async Task<DeviceProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
    {
        long started = TimeProvider.GetTimestamp();
        try
        {
            DeviceProbeResult result = await ProbeCoreAsync(cancellationToken).ConfigureAwait(false);
            return result with { ElapsedMs = ElapsedMsSince(started) };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A probe must always produce a report, never an exception (Diagnostics contract).
            return new DeviceProbeResult(false, ElapsedMsSince(started), $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>Driver-specific connect sequence. Throw <see cref="DeviceConnectFailedException"/> to control the fault detail.</summary>
    /// <param name="cancellationToken">Cancels the connect.</param>
    /// <returns>A task that completes when the device is ready.</returns>
    protected abstract Task ConnectCoreAsync(CancellationToken cancellationToken);

    /// <summary>Driver-specific teardown (close ports/sockets, release SDK handles).</summary>
    /// <param name="cancellationToken">Cancels the disconnect.</param>
    /// <returns>A task that completes when resources are released.</returns>
    protected abstract Task DisconnectCoreAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Driver-specific non-destructive self-check. The default passes while the device is
    /// <see cref="DeviceState.Ready"/> or <see cref="DeviceState.Busy"/>; drivers override
    /// with a real hardware round-trip. <c>ElapsedMs</c> is stamped by the base.
    /// </summary>
    /// <param name="cancellationToken">Cancels the probe.</param>
    /// <returns>The probe outcome (elapsed time is filled in by the base).</returns>
    protected virtual Task<DeviceProbeResult> ProbeCoreAsync(CancellationToken cancellationToken)
    {
        DeviceHealth health = _health;
        return Task.FromResult(health.State is DeviceState.Ready or DeviceState.Busy
            ? new DeviceProbeResult(true, 0, "Device connected.")
            : new DeviceProbeResult(false, 0, health.Detail ?? $"Device state is {health.State}."));
    }

    /// <summary>Updates <see cref="Health"/> and raises <see cref="HealthChanged"/> when it changed.</summary>
    /// <param name="state">The new lifecycle state.</param>
    /// <param name="detail">Optional machine-readable detail for the snapshot. Never PII.</param>
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

    /// <summary>
    /// Guards an operation entry point: throws when the device has not connected successfully.
    /// A <c>vendor_sdk_missing:*</c> fault surfaces as <see cref="DeviceNotWiredException"/>
    /// (the SDK genuinely is not wired onto this machine); any other non-ready state raises
    /// <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <param name="operation">The operation name (supplied by the compiler).</param>
    /// <exception cref="DeviceNotWiredException">The vendor SDK backing this driver is not installed.</exception>
    /// <exception cref="InvalidOperationException">The device is not in an operable state.</exception>
    protected void EnsureOperable([CallerMemberName] string operation = "")
    {
        DeviceHealth health = _health;
        if (health.State is DeviceState.Ready or DeviceState.Busy)
        {
            return;
        }

        if (health.Detail?.StartsWith("vendor_sdk_missing", StringComparison.Ordinal) == true)
        {
            throw new DeviceNotWiredException(
                $"Real driver for '{Key}' cannot run '{operation}': {health.Detail}.");
        }

        throw new InvalidOperationException(
            $"Device '{Key}' is not operable for '{operation}' — state {health.State}"
            + (health.Detail is null ? "." : $" ({health.Detail})."));
    }

    /// <summary>Releases transports and vendor SDK handles synchronously (host shutdown path).</summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Driver-specific synchronous teardown; must be idempotent.</summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    private long ElapsedMsSince(long startedTimestamp) =>
        (long)TimeProvider.GetElapsedTime(startedTimestamp).TotalMilliseconds;
}
