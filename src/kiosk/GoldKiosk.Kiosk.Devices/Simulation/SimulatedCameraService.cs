using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>Simulated camera service: every role returns a tiny embedded 1×1 PNG.</summary>
public sealed class SimulatedCameraService : SimulatedDeviceBase, ICameraService
{
    /// <summary>Initializes the simulated camera service.</summary>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays.</param>
    public SimulatedCameraService(SimulationOptions simulation, TimeProvider timeProvider)
        : base(DeviceKeys.Camera, "Camera service (simulated)", isCritical: true, simulation, timeProvider)
    {
    }

    /// <inheritdoc />
    public async Task<CapturedImage> CaptureAsync(CameraRole role, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown camera role.");
        }

        ThrowIfFaulted();
        await DelayAsync(250, cancellationToken).ConfigureAwait(false);
        return new CapturedImage(SimulatedImages.OnePixelPng, "image/png", role);
    }
}
