namespace GoldKiosk.Kiosk.Devices.Exceptions;

/// <summary>
/// Thrown by a real-driver stub whose SDK wiring lands in Phase 4 (design note §6). Reaching
/// this exception in production means a device is configured <c>Real</c> before its driver
/// exists — a provisioning error surfaced by Diagnostics, not a business failure.
/// </summary>
public sealed class DeviceNotWiredException : Exception
{
    /// <summary>Initializes a new instance with a default message.</summary>
    public DeviceNotWiredException()
        : base("The real device driver is not wired yet (Phase 4).")
    {
    }

    /// <summary>Initializes a new instance with the given message.</summary>
    /// <param name="message">The error message.</param>
    public DeviceNotWiredException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with the given message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The causing exception.</param>
    public DeviceNotWiredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance for a specific device operation.</summary>
    /// <param name="deviceKey">The canonical device key (see <c>DeviceKeys</c>).</param>
    /// <param name="operation">The operation that was attempted.</param>
    public DeviceNotWiredException(string deviceKey, string operation)
        : base($"Real driver for '{deviceKey}' is not wired yet (Phase 4) — operation '{operation}' is unavailable.")
    {
        DeviceKey = deviceKey;
        Operation = operation;
    }

    /// <summary>The canonical key of the unwired device, when known.</summary>
    public string? DeviceKey { get; }

    /// <summary>The operation that was attempted, when known.</summary>
    public string? Operation { get; }
}
