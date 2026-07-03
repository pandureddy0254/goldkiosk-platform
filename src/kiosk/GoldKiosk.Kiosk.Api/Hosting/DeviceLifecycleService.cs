using GoldKiosk.Kiosk.Api.Logging;
using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Api.Hosting;

/// <summary>
/// Connects every composed device at startup and disconnects at shutdown. Logs the
/// effective mode per device — the ADR 0004 startup requirement — and warns loudly when
/// any device is mocked (the kiosk then trades in test mode only).
/// </summary>
/// <param name="registry">The composed device registry.</param>
/// <param name="logger">The host logger.</param>
public sealed class DeviceLifecycleService(
    IDeviceRegistry registry,
    ILogger<DeviceLifecycleService> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (IKioskDevice device in registry.All)
        {
            logger.DeviceComposed(device.Key, device.Mode, device.DisplayName, device.IsCritical);
        }

        if (registry.All.Any(d => d.Mode == DeviceMode.Mock))
        {
            logger.MockedDevicesInUse();
        }

        foreach (IKioskDevice device in registry.All)
        {
            await device.ConnectAsync(cancellationToken);
            logger.DeviceConnected(device.Key, device.Health.State, device.Health.Detail);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (IKioskDevice device in registry.All)
        {
            try
            {
                await device.DisconnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Deliberate catch-all: shutdown must disconnect every remaining device
                // even when one throws; each failure is logged individually.
                logger.DeviceDisconnectFailed(ex, device.Key);
            }
        }
    }
}
