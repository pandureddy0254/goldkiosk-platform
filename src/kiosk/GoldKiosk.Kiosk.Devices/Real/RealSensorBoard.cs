using System.Reflection;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Exceptions;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Vendor;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real digital-input sensor board: Advantech USB-4761 via the vendor's managed
/// <c>Automation.BDaq</c> assembly, loaded by reflection (never compile-time referenced).
/// Bit map from the legacy <c>CheckSensorStatus</c>: bit 0 tray (high = closed), bit 1
/// chamber (high = open), bit 2 UPS on mains, bit 3 chamber cup, bit 4 scale cup. When the
/// SDK is absent the device faults with <c>vendor_sdk_missing:Automation.BDaq</c> at connect
/// — composition never throws.
/// </summary>
public sealed class RealSensorBoard : RealDeviceBase, ISensorBoard
{
    private const string SdkName = "Automation.BDaq";

    private readonly ConnectionOptions _connection;
    private object? _diCtrl;
    private MethodInfo? _readMethod;

    /// <summary>Initializes the driver with legacy-parity defaults.</summary>
    public RealSensorBoard()
        : this(null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="connection">Connection overrides (vendor assembly path); <see langword="null"/> uses <see cref="RealDeviceDefaults"/>.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealSensorBoard(ConnectionOptions? connection, TimeProvider? timeProvider = null)
        : base(DeviceKeys.SensorBoard, "Sensor board (Advantech USB-4761 DI)", isCritical: true, timeProvider)
    {
        _connection = (connection ?? new ConnectionOptions()).MergedWith(RealDeviceDefaults.For(DeviceKeys.SensorBoard));
    }

    /// <inheritdoc />
    public async Task<SensorSnapshot> ReadAsync(CancellationToken cancellationToken = default)
    {
        EnsureOperable();
        byte portData = await Task.Run(ReadPortZero, cancellationToken).ConfigureAwait(false);
        return new SensorSnapshot(
            TrayClosed: IsBitSet(portData, 0),
            ChamberClosed: !IsBitSet(portData, 1),
            UpsOnMains: IsBitSet(portData, 2),
            ChamberCupPresent: IsBitSet(portData, 3),
            ScaleCupPresent: IsBitSet(portData, 4));
    }

    /// <inheritdoc />
    protected override Task ConnectCoreAsync(CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            Assembly sdk = VendorSdkLoader.LoadAssembly(_connection.VendorAssemblyPath!, SdkName);
            Type ctrlType = VendorSdkLoader.GetRequiredType(sdk, "Automation.BDaq.InstantDiCtrl", SdkName);
            Type deviceInfoType = VendorSdkLoader.GetRequiredType(sdk, "Automation.BDaq.DeviceInformation", SdkName);

            object ctrl = Activator.CreateInstance(ctrlType)
                ?? throw new DeviceConnectFailedException($"{VendorSdkLoader.MissingDetail(SdkName)}:InstantDiCtrl");
            object deviceInfo = Activator.CreateInstance(deviceInfoType, RealDeviceDefaults.AdvantechDeviceDescription)
                ?? throw new DeviceConnectFailedException($"{VendorSdkLoader.MissingDetail(SdkName)}:DeviceInformation");
            VendorSdkLoader.SetProperty(ctrl, "SelectedDevice", deviceInfo);

            if (VendorSdkLoader.GetProperty(ctrl, "Initialized") is not true)
            {
                throw new DeviceConnectFailedException(
                    $"advantech_device_not_found:{RealDeviceDefaults.AdvantechDeviceDescription}");
            }

            _readMethod = ctrlType.GetMethod("Read", [typeof(int), typeof(byte).MakeByRefType()])
                ?? throw new DeviceConnectFailedException($"{VendorSdkLoader.MissingDetail(SdkName)}:Read");
            _diCtrl = ctrl;
        }, cancellationToken);

    /// <inheritdoc />
    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        ReleaseControl();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<DeviceProbeResult> ProbeCoreAsync(CancellationToken cancellationToken)
    {
        if (_diCtrl is null)
        {
            return new DeviceProbeResult(false, 0, Health.Detail ?? "Sensor board is not connected.");
        }

        byte portData = await Task.Run(ReadPortZero, cancellationToken).ConfigureAwait(false);
        return new DeviceProbeResult(true, 0, $"Digital inputs read (0x{portData:X2}).");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseControl();
        }

        base.Dispose(disposing);
    }

    private static bool IsBitSet(byte value, int bit) => ((value >> bit) & 0x1) == 1;

    private void ReleaseControl()
    {
        if (_diCtrl is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _diCtrl = null;
        _readMethod = null;
    }

    private byte ReadPortZero()
    {
        object ctrl = _diCtrl ?? throw new InvalidOperationException("sensor_board_not_connected");
        MethodInfo read = _readMethod ?? throw new InvalidOperationException("sensor_board_not_connected");

        object?[] args = [0, (byte)0];
        object? errorCode = read.Invoke(ctrl, args);
        if (errorCode?.ToString() is { } code && !string.Equals(code, "Success", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"advantech_read_error:{code}");
        }

        return (byte)args[1]!;
    }
}
