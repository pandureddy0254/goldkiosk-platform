using System.Reflection;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Exceptions;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Vendor;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real camera service: DirectShow capture via the reflection-loaded <c>Camera_NET.dll</c>
/// wrapper (legacy <c>GCCamera</c> path: <c>CameraChoice.UpdateDeviceList()</c>, per capture
/// <c>SetCamera(moniker)</c> → warm-up wait → <c>SnapshotSourceImage()</c> →
/// <c>CloseCamera()</c>). Camera roles map to DirectShow device indexes via
/// <see cref="CameraServiceOptions.RoleIndexes"/>. Captures are serialized — the wrapper owns
/// a single capture graph. Bitmap encoding to PNG runs through reflection over the
/// System.Drawing assembly the wrapper brings along, so this library carries no drawing
/// reference. SDK absent → <c>vendor_sdk_missing:Camera_NET</c>. Captured images are
/// PII-adjacent audit artifacts: persisted by the caller, never logged.
/// </summary>
public sealed class RealCameraService : RealDeviceBase, ICameraService
{
    private const string SdkName = "Camera_NET";

    private readonly ConnectionOptions _connection;
    private readonly CameraServiceOptions _options;
    private readonly SemaphoreSlim _captureGate = new(1, 1);
    private object? _cameraChoice;
    private object? _cameraControl;

    /// <summary>Initializes the driver with legacy-parity defaults.</summary>
    public RealCameraService()
        : this(null, null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="connection">Connection overrides (SDK path); <see langword="null"/> uses <see cref="RealDeviceDefaults"/>.</param>
    /// <param name="options">Role-to-index mapping and warm-up; <see langword="null"/> uses defaults.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealCameraService(
        ConnectionOptions? connection,
        CameraServiceOptions? options = null,
        TimeProvider? timeProvider = null)
        : base(DeviceKeys.Camera, "Camera service (DirectShow)", isCritical: true, timeProvider)
    {
        _connection = (connection ?? new ConnectionOptions()).MergedWith(RealDeviceDefaults.For(DeviceKeys.Camera));
        _options = options ?? new CameraServiceOptions();
    }

    /// <inheritdoc />
    public async Task<CapturedImage> CaptureAsync(CameraRole role, CancellationToken cancellationToken = default)
    {
        EnsureOperable();
        if (!_options.RoleIndexes.TryGetValue(role.ToString(), out int cameraIndex))
        {
            throw new InvalidOperationException($"camera_role_unmapped:{role}");
        }

        await _captureGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        SetHealth(DeviceState.Busy, $"Capturing {role}.");
        try
        {
            await Task.Run(() => SelectCamera(cameraIndex), cancellationToken).ConfigureAwait(false);
            if (_options.WarmupDelayMs > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(_options.WarmupDelayMs), TimeProvider, cancellationToken)
                    .ConfigureAwait(false);
            }

            byte[] pngBytes = await Task.Run(SnapshotToPng, cancellationToken).ConfigureAwait(false);
            SetHealth(DeviceState.Ready);
            return new CapturedImage(pngBytes, "image/png", role);
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Ready);
            throw;
        }
        catch (Exception ex)
        {
            SetHealth(DeviceState.Faulted, $"camera_capture_failed:{ex.GetType().Name}");
            throw;
        }
        finally
        {
            _captureGate.Release();
        }
    }

    /// <inheritdoc />
    protected override Task ConnectCoreAsync(CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            Assembly sdk = VendorSdkLoader.LoadAssembly(_connection.VendorAssemblyPath!, SdkName);
            Type choiceType = VendorSdkLoader.GetRequiredType(sdk, "Camera_NET.CameraChoice", SdkName);
            Type controlType = VendorSdkLoader.GetRequiredType(sdk, "Camera_NET.CameraControl", SdkName);

            object choice = Activator.CreateInstance(choiceType)
                ?? throw new DeviceConnectFailedException(VendorSdkLoader.MissingDetail(SdkName));
            VendorSdkLoader.Invoke(choice, "UpdateDeviceList");
            if (GetDeviceCount(choice) == 0)
            {
                throw new DeviceConnectFailedException("camera_none_detected");
            }

            _cameraControl = Activator.CreateInstance(controlType)
                ?? throw new DeviceConnectFailedException(VendorSdkLoader.MissingDetail(SdkName));
            _cameraChoice = choice;
        }, cancellationToken);

    /// <inheritdoc />
    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        ReleaseSdk();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task<DeviceProbeResult> ProbeCoreAsync(CancellationToken cancellationToken)
    {
        if (_cameraChoice is null)
        {
            return Task.FromResult(new DeviceProbeResult(false, 0, Health.Detail ?? "Cameras are not connected."));
        }

        int detected = GetDeviceCount(_cameraChoice);
        int required = _options.RoleIndexes.Count == 0 ? 0 : _options.RoleIndexes.Values.Max() + 1;
        return Task.FromResult(detected >= required
            ? new DeviceProbeResult(true, 0, $"{detected} cameras detected.")
            : new DeviceProbeResult(false, 0, $"camera_count_insufficient:{detected}<{required}"));
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseSdk();
            _captureGate.Dispose();
        }

        base.Dispose(disposing);
    }

    private static int GetDeviceCount(object cameraChoice) =>
        VendorSdkLoader.GetProperty(cameraChoice, "Devices") is System.Collections.ICollection devices
            ? devices.Count
            : 0;

    private static object DeviceAt(object cameraChoice, int index)
    {
        if (VendorSdkLoader.GetProperty(cameraChoice, "Devices") is not System.Collections.IList devices
            || index < 0
            || index >= devices.Count)
        {
            throw new InvalidOperationException($"camera_index_out_of_range:{index}");
        }

        return devices[index] ?? throw new InvalidOperationException($"camera_index_out_of_range:{index}");
    }

    private void SelectCamera(int cameraIndex)
    {
        object choice = _cameraChoice ?? throw new InvalidOperationException("camera_not_connected");
        object control = _cameraControl ?? throw new InvalidOperationException("camera_not_connected");

        object device = DeviceAt(choice, cameraIndex);
        object? moniker = VendorSdkLoader.GetProperty(device, "Mon");
        TryCloseCamera(control);
        VendorSdkLoader.Invoke(control, "SetCamera", moniker, null);
    }

    private byte[] SnapshotToPng()
    {
        object control = _cameraControl ?? throw new InvalidOperationException("camera_not_connected");
        object bitmap = VendorSdkLoader.Invoke(control, "SnapshotSourceImage")
            ?? throw new InvalidOperationException("camera_snapshot_null");
        try
        {
            Type bitmapType = bitmap.GetType();
            Type imageFormatType = bitmapType.Assembly.GetType("System.Drawing.Imaging.ImageFormat")
                ?? throw new InvalidOperationException("camera_imageformat_unresolved");
            object png = imageFormatType.GetProperty("Png", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null)!;
            MethodInfo save = bitmapType.GetMethod("Save", [typeof(Stream), imageFormatType])
                ?? throw new InvalidOperationException("camera_bitmap_save_unresolved");

            using var stream = new MemoryStream();
            save.Invoke(bitmap, [stream, png]);
            return stream.ToArray();
        }
        finally
        {
            (bitmap as IDisposable)?.Dispose();
            TryCloseCamera(control);
        }
    }

    private static void TryCloseCamera(object control)
    {
        try
        {
            VendorSdkLoader.Invoke(control, "CloseCamera");
        }
        catch (TargetInvocationException)
        {
            // Closing an already-closed graph is benign (legacy did the same blind close).
        }
    }

    private void ReleaseSdk()
    {
        if (_cameraControl is not null)
        {
            TryCloseCamera(_cameraControl);
        }

        (_cameraControl as IDisposable)?.Dispose();
        _cameraControl = null;
        (_cameraChoice as IDisposable)?.Dispose();
        _cameraChoice = null;
    }
}
