namespace GoldKiosk.Kiosk.Devices.Exceptions;

/// <summary>
/// Thrown by a simulated device whose key is listed in
/// <c>Devices:Simulation:FaultDevices</c>. Connection succeeds but operations throw, so the
/// session flow reaches the operation and exercises the error-recovery path (ADR 0004
/// scripted failure modes).
/// </summary>
public sealed class SimulatedDeviceFaultException : Exception
{
    /// <summary>Initializes a new instance with a default message.</summary>
    public SimulatedDeviceFaultException()
        : base("A simulated device fault was injected.")
    {
    }

    /// <summary>Initializes a new instance with the given message.</summary>
    /// <param name="message">The error message.</param>
    public SimulatedDeviceFaultException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with the given message and inner exception.</summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The causing exception.</param>
    public SimulatedDeviceFaultException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance for a specific device operation.</summary>
    /// <param name="deviceKey">The canonical device key (see <c>DeviceKeys</c>).</param>
    /// <param name="operation">The operation during which the fault was injected.</param>
    public SimulatedDeviceFaultException(string deviceKey, string operation)
        : base($"Simulated fault injected on '{deviceKey}' during '{operation}'.")
    {
        DeviceKey = deviceKey;
        Operation = operation;
    }

    /// <summary>The canonical key of the faulted device, when known.</summary>
    public string? DeviceKey { get; }

    /// <summary>The operation during which the fault was injected, when known.</summary>
    public string? Operation { get; }
}
