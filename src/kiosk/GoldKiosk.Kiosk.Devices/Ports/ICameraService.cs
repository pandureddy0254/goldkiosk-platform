using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// Role-keyed camera port — one service fronts every physical camera. Real target:
/// DirectShow capture with the camera index (and optional ROI) per role coming from
/// machine configuration.
/// </summary>
public interface ICameraService : IKioskDevice
{
    /// <summary>Captures a still image for the given role.</summary>
    /// <param name="role">Which camera/viewpoint to capture.</param>
    /// <param name="cancellationToken">Cancels the capture.</param>
    /// <returns>The captured image bytes with media type.</returns>
    Task<CapturedImage> CaptureAsync(CameraRole role, CancellationToken cancellationToken = default);
}
