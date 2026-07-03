using System.Reflection;
using System.Text;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Exceptions;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Vendor;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real FlexCode fingerprint scanner driver, reflection-gated over
/// <c>Interop.FlexCodeSDK.dll</c>. Ported from the legacy <c>FPScanner</c> sidecar:
/// <c>FinFPReg.AddDeviceInfo(serial, verificationCode, activationCode)</c> at connect, then
/// per capture <c>FPRegistrationStart(secret)</c> with completion via the COM events
/// <c>FPSamplesNeeded(short)</c> (a sample was accepted, more needed),
/// <c>FPRegistrationTemplate(string)</c> (enrolment complete) and
/// <c>FPRegistrationStatus</c> (failure states). Events are bridged without compile-time
/// delegate types via <see cref="ReflectionEventBridge"/>. Templates are biometric PII —
/// never logged, never in health details.
/// </summary>
public sealed class RealFingerprintScanner : RealDeviceBase, IFingerprintScanner
{
    private const string SdkName = "Interop.FlexCodeSDK";

    private static readonly string[] BenignStatuses =
        ["r_OK", "r_RegistrationCaptureStart", "r_RegistrationCaptureStop"];

    private readonly ConnectionOptions _connection;
    private readonly FingerprintScannerOptions _options;
    private readonly Lock _captureLock = new();
    private readonly List<ReflectionEventBridge> _subscriptions = [];
    private object? _registration;
    private TaskCompletionSource<FingerprintResult>? _captureTcs;
    private int _lastSamplesNeeded;

    /// <summary>Initializes the driver with legacy-parity defaults.</summary>
    public RealFingerprintScanner()
        : this(null, null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="connection">Connection overrides (SDK path); <see langword="null"/> uses <see cref="RealDeviceDefaults"/>.</param>
    /// <param name="options">Scanner licensing and capture tuning; <see langword="null"/> uses defaults.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealFingerprintScanner(
        ConnectionOptions? connection,
        FingerprintScannerOptions? options = null,
        TimeProvider? timeProvider = null)
        : base(DeviceKeys.FingerprintScanner, "Fingerprint scanner (FlexCode)", isCritical: false, timeProvider)
    {
        _connection = (connection ?? new ConnectionOptions())
            .MergedWith(RealDeviceDefaults.For(DeviceKeys.FingerprintScanner));
        _options = options ?? new FingerprintScannerOptions();
    }

    /// <inheritdoc />
    public async Task<FingerprintResult> CaptureAsync(CancellationToken cancellationToken = default)
    {
        EnsureOperable();
        object registration = _registration ?? throw new InvalidOperationException("fingerprint_not_connected");

        TaskCompletionSource<FingerprintResult> captureTcs;
        lock (_captureLock)
        {
            captureTcs = new TaskCompletionSource<FingerprintResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _captureTcs = captureTcs;
        }

        SetHealth(DeviceState.Busy, "Capturing fingerprint.");
        try
        {
            await Task.Run(
                    () => VendorSdkLoader.Invoke(registration, "FPRegistrationStart", _options.RegistrationSecret),
                    cancellationToken)
                .ConfigureAwait(false);

            FingerprintResult result;
            try
            {
                result = await captureTcs.Task
                    .WaitAsync(TimeSpan.FromMilliseconds(_options.CaptureTimeoutMs), TimeProvider, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                result = new FingerprintResult(false, Math.Max(1, _lastSamplesNeeded), null);
            }

            SetHealth(DeviceState.Ready);
            return result;
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Ready);
            throw;
        }
        catch (Exception ex)
        {
            SetHealth(DeviceState.Faulted, $"fingerprint_capture_failed:{ex.GetType().Name}");
            throw;
        }
        finally
        {
            lock (_captureLock)
            {
                _captureTcs = null;
            }
        }
    }

    /// <inheritdoc />
    protected override Task ConnectCoreAsync(CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            Assembly sdk = VendorSdkLoader.LoadAssembly(_connection.VendorAssemblyPath!, SdkName);
            Type regType = sdk.GetType("FlexCodeSDK.FinFPRegClass", throwOnError: false)
                ?? VendorSdkLoader.GetRequiredType(sdk, "FlexCodeSDK.FinFPReg", SdkName);
            object registration = Activator.CreateInstance(regType)
                ?? throw new DeviceConnectFailedException(VendorSdkLoader.MissingDetail(SdkName));

            if (string.IsNullOrWhiteSpace(_options.SerialNumber))
            {
                throw new DeviceConnectFailedException("flexcode_device_credentials_missing");
            }

            VendorSdkLoader.Invoke(
                registration,
                "AddDeviceInfo",
                _options.SerialNumber,
                _options.VerificationCode,
                _options.ActivationCode);

            Subscribe(registration, "FPSamplesNeeded", OnSamplesNeeded);
            Subscribe(registration, "FPRegistrationTemplate", OnTemplate);
            Subscribe(registration, "FPRegistrationStatus", OnStatus);
            _registration = registration;
        }, cancellationToken);

    /// <inheritdoc />
    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        ReleaseSdk();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseSdk();
        }

        base.Dispose(disposing);
    }

    private static byte[] DecodeTemplate(string template)
    {
        // FlexCode emits the enrolment template as text; base64 payloads decode to raw
        // bytes, anything else is preserved verbatim as UTF-8.
        Span<byte> buffer = new byte[(template.Length * 3 / 4) + 4];
        return Convert.TryFromBase64String(template, buffer, out int written)
            ? buffer[..written].ToArray()
            : Encoding.UTF8.GetBytes(template);
    }

    private void Subscribe(object source, string eventName, Action<object?[]> callback)
    {
        if (ReflectionEventBridge.TrySubscribe(source, eventName, callback) is { } bridge)
        {
            _subscriptions.Add(bridge);
        }
    }

    private void OnSamplesNeeded(object?[] args)
    {
        int samples = args.Length > 0
            ? Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture)
            : 0;
        _lastSamplesNeeded = samples;
        _captureTcs?.TrySetResult(new FingerprintResult(true, Math.Max(1, samples), null));
    }

    private void OnTemplate(object?[] args)
    {
        string template = args.Length > 0 ? args[0] as string ?? string.Empty : string.Empty;
        _captureTcs?.TrySetResult(template.Length > 0
            ? new FingerprintResult(true, 1, DecodeTemplate(template))
            : new FingerprintResult(false, Math.Max(1, _lastSamplesNeeded), null));
    }

    private void OnStatus(object?[] args)
    {
        string status = args.Length > 0 ? args[0]?.ToString() ?? string.Empty : string.Empty;
        if (BenignStatuses.Contains(status, StringComparer.Ordinal))
        {
            return;
        }

        _captureTcs?.TrySetResult(new FingerprintResult(false, Math.Max(1, _lastSamplesNeeded), null));
    }

    private void ReleaseSdk()
    {
        foreach (ReflectionEventBridge subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        if (_registration is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _registration = null;
    }
}
