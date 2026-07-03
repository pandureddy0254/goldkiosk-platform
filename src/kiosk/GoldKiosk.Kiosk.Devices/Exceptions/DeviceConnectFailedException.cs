namespace GoldKiosk.Kiosk.Devices.Exceptions;

/// <summary>
/// Thrown inside a real driver's connect sequence to carry a machine-readable health detail
/// (e.g. <c>vendor_sdk_missing:Automation.BDaq</c>). <c>RealDeviceBase.ConnectAsync</c>
/// catches it and surfaces the message as the <c>Faulted</c> health detail — connect never
/// crashes the composed device set.
/// </summary>
public sealed class DeviceConnectFailedException : Exception
{
    /// <summary>Initializes a new instance with a default message.</summary>
    public DeviceConnectFailedException()
        : base("The real device driver failed to connect.")
    {
    }

    /// <summary>Initializes a new instance whose message becomes the health detail.</summary>
    /// <param name="message">Machine-readable failure detail. Never PII.</param>
    public DeviceConnectFailedException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance with the given detail and cause.</summary>
    /// <param name="message">Machine-readable failure detail. Never PII.</param>
    /// <param name="innerException">The causing exception.</param>
    public DeviceConnectFailedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
