using GoldKiosk.Contracts.V1.Devices;
using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Api.Devices;

/// <summary>
/// Maps device port state onto the wire vocabulary of <c>GET /devices</c>
/// (payload samples §12).
/// </summary>
internal static class DeviceStatusMapper
{
    /// <summary>Builds the wire status for one device.</summary>
    /// <param name="device">The device to describe.</param>
    /// <returns>The wire DTO.</returns>
    public static DeviceStatusDto ToDto(IKioskDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        return new DeviceStatusDto(
            Key: device.Key,
            Mode: device.Mode == DeviceMode.Mock ? "mock" : "real",
            State: ToWireState(device.Health.State),
            Detail: device.Health.Detail);
    }

    /// <summary>Aggregates the fleet health: <c>healthy</c> when every device is usable, else <c>degraded</c>.</summary>
    /// <param name="devices">The composed device set.</param>
    /// <returns>The aggregate health string.</returns>
    public static string Overall(IReadOnlyList<IKioskDevice> devices)
    {
        ArgumentNullException.ThrowIfNull(devices);
        return devices.All(d => d.Health.State is DeviceState.Ready or DeviceState.Busy)
            ? "healthy"
            : "degraded";
    }

    private static string ToWireState(DeviceState state) => state switch
    {
        DeviceState.NotInitialized => "not_initialized",
        DeviceState.Connecting => "connecting",
        DeviceState.Ready => "ready",
        DeviceState.Busy => "busy",
        DeviceState.Faulted => "faulted",
        DeviceState.Disconnected => "disconnected",
        _ => "unknown",
    };
}
