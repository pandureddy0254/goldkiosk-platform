using System.Reflection;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Exceptions;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Vendor;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real power relay driver: Advantech USB-4761 digital outputs via the reflection-loaded
/// <c>Automation.BDaq</c> assembly. Relay patterns are the legacy <c>ClientConfigProvider</c>
/// command bytes: 3 = device rail on, 0 = all off, 7 = Vanta-gun toggle. The analyser
/// power-cycle (toggle → dwell → rail on → dwell) is the legacy recovery path for a wedged
/// XRF gun (<c>ToggleVantaGunPower</c>). SDK absent → <c>vendor_sdk_missing:Automation.BDaq</c>.
/// </summary>
public sealed class RealPowerRelays : RealDeviceBase, IPowerRelays
{
    private const string SdkName = "Automation.BDaq";

    /// <summary>Legacy <c>PowerOnDevicesCmd</c>: energize the shared device rail.</summary>
    private const byte RailOnPattern = 3;

    /// <summary>Legacy <c>PowerOffDevicesCmd</c>: everything off.</summary>
    private const byte RailOffPattern = 0;

    /// <summary>Legacy <c>ToggleVantaGunCmd</c>: pulse the analyser power relay.</summary>
    private const byte AnalyserTogglePattern = 7;

    private const int AnalyserCycleDwellMs = 5000;

    private readonly ConnectionOptions _connection;
    private object? _doCtrl;
    private MethodInfo? _writeMethod;

    /// <summary>Initializes the driver with legacy-parity defaults.</summary>
    public RealPowerRelays()
        : this(null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="connection">Connection overrides (vendor assembly path); <see langword="null"/> uses <see cref="RealDeviceDefaults"/>.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealPowerRelays(ConnectionOptions? connection, TimeProvider? timeProvider = null)
        : base(DeviceKeys.PowerRelays, "Power relays (Advantech USB-4761 DO)", isCritical: true, timeProvider)
    {
        _connection = (connection ?? new ConnectionOptions()).MergedWith(RealDeviceDefaults.For(DeviceKeys.PowerRelays));
    }

    /// <inheritdoc />
    public async Task SetDevicePowerAsync(bool on, CancellationToken cancellationToken = default)
    {
        EnsureOperable();
        SetHealth(DeviceState.Busy, on ? "Energizing device rail." : "De-energizing device rail.");
        try
        {
            await WritePatternAsync(on ? RailOnPattern : RailOffPattern, cancellationToken).ConfigureAwait(false);
            SetHealth(DeviceState.Ready);
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Ready);
            throw;
        }
        catch (Exception ex)
        {
            SetHealth(DeviceState.Faulted, $"relay_write_failed:{ex.GetType().Name}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task ToggleAnalyserPowerAsync(CancellationToken cancellationToken = default)
    {
        EnsureOperable();
        SetHealth(DeviceState.Busy, "Power-cycling analyser.");
        try
        {
            await WritePatternAsync(AnalyserTogglePattern, cancellationToken).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromMilliseconds(AnalyserCycleDwellMs), TimeProvider, cancellationToken)
                .ConfigureAwait(false);
            await WritePatternAsync(RailOnPattern, cancellationToken).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromMilliseconds(AnalyserCycleDwellMs), TimeProvider, cancellationToken)
                .ConfigureAwait(false);
            SetHealth(DeviceState.Ready);
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Ready);
            throw;
        }
        catch (Exception ex)
        {
            SetHealth(DeviceState.Faulted, $"relay_write_failed:{ex.GetType().Name}");
            throw;
        }
    }

    /// <inheritdoc />
    protected override Task ConnectCoreAsync(CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            Assembly sdk = VendorSdkLoader.LoadAssembly(_connection.VendorAssemblyPath!, SdkName);
            Type ctrlType = VendorSdkLoader.GetRequiredType(sdk, "Automation.BDaq.InstantDoCtrl", SdkName);
            Type deviceInfoType = VendorSdkLoader.GetRequiredType(sdk, "Automation.BDaq.DeviceInformation", SdkName);

            object ctrl = Activator.CreateInstance(ctrlType)
                ?? throw new DeviceConnectFailedException($"{VendorSdkLoader.MissingDetail(SdkName)}:InstantDoCtrl");
            object deviceInfo = Activator.CreateInstance(deviceInfoType, RealDeviceDefaults.AdvantechDeviceDescription)
                ?? throw new DeviceConnectFailedException($"{VendorSdkLoader.MissingDetail(SdkName)}:DeviceInformation");
            VendorSdkLoader.SetProperty(ctrl, "SelectedDevice", deviceInfo);

            if (VendorSdkLoader.GetProperty(ctrl, "Initialized") is not true)
            {
                throw new DeviceConnectFailedException(
                    $"advantech_device_not_found:{RealDeviceDefaults.AdvantechDeviceDescription}");
            }

            _writeMethod = ctrlType.GetMethod("Write", [typeof(int), typeof(byte)])
                ?? throw new DeviceConnectFailedException($"{VendorSdkLoader.MissingDetail(SdkName)}:Write");
            _doCtrl = ctrl;
        }, cancellationToken);

    /// <inheritdoc />
    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        ReleaseControl();
        return Task.CompletedTask;
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

    private void ReleaseControl()
    {
        if (_doCtrl is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _doCtrl = null;
        _writeMethod = null;
    }

    private Task WritePatternAsync(byte pattern, CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            object ctrl = _doCtrl ?? throw new InvalidOperationException("power_relays_not_connected");
            MethodInfo write = _writeMethod ?? throw new InvalidOperationException("power_relays_not_connected");

            object? errorCode = write.Invoke(ctrl, [0, pattern]);
            if (errorCode?.ToString() is { } code && !string.Equals(code, "Success", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"advantech_write_error:{code}");
            }
        }, cancellationToken);
}
