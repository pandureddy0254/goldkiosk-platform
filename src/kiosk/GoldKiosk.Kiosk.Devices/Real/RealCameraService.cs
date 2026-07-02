using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real camera service driver stub. Phase 4 target SDK: DirectShow capture with the camera
/// index (and optional ROI) per <see cref="CameraRole"/> from machine configuration.
/// </summary>
public sealed class RealCameraService : RealDeviceStub, ICameraService
{
    /// <summary>Initializes the stub.</summary>
    public RealCameraService()
        : base(DeviceKeys.Camera, "Camera service (DirectShow)", isCritical: true)
    {
    }

    /// <inheritdoc />
    public Task<CapturedImage> CaptureAsync(CameraRole role, CancellationToken cancellationToken = default) =>
        throw NotWired();
}
