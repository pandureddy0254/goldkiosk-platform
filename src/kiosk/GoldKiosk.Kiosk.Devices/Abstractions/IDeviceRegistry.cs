using System.Diagnostics.CodeAnalysis;

namespace GoldKiosk.Kiosk.Devices.Abstractions;

/// <summary>
/// Read-only lookup over the composed device set. The Kiosk.Api host builds one
/// <c>Registry.DeviceRegistry</c> at startup (after resolving Real/Mock per device from
/// <c>DevicesOptions</c>) and registers it as a singleton; endpoints and the session engine
/// resolve devices through this abstraction, never by constructing them.
/// </summary>
public interface IDeviceRegistry
{
    /// <summary>All registered devices, in registration order.</summary>
    IReadOnlyList<IKioskDevice> All { get; }

    /// <summary>Gets the device registered under the given canonical key.</summary>
    /// <param name="key">A key from <see cref="DeviceKeys"/> (case-insensitive).</param>
    /// <returns>The registered device.</returns>
    /// <exception cref="KeyNotFoundException">No device is registered under <paramref name="key"/>.</exception>
    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
        Justification = "'Get' is the registry contract name fixed by the GK-2 design note; VB interop is not a consumer of this edge library.")]
    IKioskDevice Get(string key);

    /// <summary>Gets the first registered device implementing the given port interface.</summary>
    /// <typeparam name="T">The device port interface (e.g. a scale or analyser port).</typeparam>
    /// <returns>The registered device cast to <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">No registered device implements <typeparamref name="T"/>.</exception>
    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords",
        Justification = "'Get' is the registry contract name fixed by the GK-2 design note; VB interop is not a consumer of this edge library.")]
    T Get<T>()
        where T : class, IKioskDevice;
}
