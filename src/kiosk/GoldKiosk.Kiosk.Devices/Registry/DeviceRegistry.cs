using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Registry;

/// <summary>
/// Immutable <see cref="IDeviceRegistry"/> over the device set composed at startup. The
/// Kiosk.Api host builds one instance after resolving Real/Mock per device
/// (<c>DevicesOptions.ResolveMode</c>) and registers it as a singleton.
/// </summary>
public sealed class DeviceRegistry : IDeviceRegistry
{
    private readonly IReadOnlyList<IKioskDevice> _devices;
    private readonly Dictionary<string, IKioskDevice> _byKey;

    /// <summary>Initializes the registry over the composed device set.</summary>
    /// <param name="devices">The devices, one per key; keys must be unique (case-insensitive).</param>
    /// <exception cref="ArgumentException">A device is null or two devices share a key.</exception>
    public DeviceRegistry(IReadOnlyList<IKioskDevice> devices)
    {
        ArgumentNullException.ThrowIfNull(devices);

        var byKey = new Dictionary<string, IKioskDevice>(devices.Count, StringComparer.OrdinalIgnoreCase);
        foreach (IKioskDevice device in devices)
        {
            if (device is null)
            {
                throw new ArgumentException("Device list must not contain null entries.", nameof(devices));
            }

            if (!byKey.TryAdd(device.Key, device))
            {
                throw new ArgumentException($"Duplicate device key '{device.Key}'.", nameof(devices));
            }
        }

        _devices = [.. devices];
        _byKey = byKey;
    }

    /// <inheritdoc />
    public IReadOnlyList<IKioskDevice> All => _devices;

    /// <inheritdoc />
    public IKioskDevice Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _byKey.TryGetValue(key, out IKioskDevice? device)
            ? device
            : throw new KeyNotFoundException($"No device registered with key '{key}'.");
    }

    /// <inheritdoc />
    public T Get<T>()
        where T : class, IKioskDevice
    {
        foreach (IKioskDevice device in _devices)
        {
            if (device is T typed)
            {
                return typed;
            }
        }

        throw new InvalidOperationException($"No device registered implementing {typeof(T).Name}.");
    }
}
