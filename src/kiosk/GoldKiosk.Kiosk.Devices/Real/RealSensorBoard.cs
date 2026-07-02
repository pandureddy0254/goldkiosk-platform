using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real sensor board driver stub. Phase 4 target SDK: Advantech USB-4761 DAQ digital inputs
/// (bit map: tray, chamber, UPS, chamber cup, scale cup).
/// </summary>
public sealed class RealSensorBoard : RealDeviceStub, ISensorBoard
{
    /// <summary>Initializes the stub.</summary>
    public RealSensorBoard()
        : base(DeviceKeys.SensorBoard, "Sensor board (Advantech USB-4761)", isCritical: true)
    {
    }

    /// <inheritdoc />
    public Task<SensorSnapshot> ReadAsync(CancellationToken cancellationToken = default) =>
        throw NotWired();
}
