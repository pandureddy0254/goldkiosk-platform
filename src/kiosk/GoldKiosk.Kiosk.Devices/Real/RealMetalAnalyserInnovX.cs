using System.Runtime.InteropServices;
using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;
using GoldKiosk.Kiosk.Devices.Real.Vendor;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Legacy Innov-X XRF driver shell: the gun is driven through a late-bound COM automation
/// object resolved by ProgID (<c>Devices:Overrides:metal_analyser</c> with
/// <c>Connection:Variant = innovx</c>; ProgID from
/// <see cref="MetalAnalyserOptions.InnovXProgId"/>). The legacy Innov-X call sequence lived
/// in the out-of-scope <c>XrfComApplication</c> sidecar, so this driver implements the COM
/// lifecycle (create/release, fault with <c>vendor_sdk_missing:{ProgID}</c> when
/// unregistered) and a guarded <c>StartTest</c> invocation; the result-retrieval member
/// binding is completed during Phase-4 hardware-lab validation. Until then a run returns a
/// failed <see cref="AnalysisRun"/> with a machine-readable reason — never a wrong reading.
/// </summary>
public sealed class RealMetalAnalyserInnovX : RealDeviceBase, IMetalAnalyser
{
    private readonly MetalAnalyserOptions _options;
    private object? _comInstance;

    /// <summary>Initializes the driver with default options.</summary>
    public RealMetalAnalyserInnovX()
        : this(null, null)
    {
    }

    /// <summary>Initializes the driver.</summary>
    /// <param name="options">Analyser tuning (Innov-X ProgID); <see langword="null"/> uses defaults.</param>
    /// <param name="timeProvider">Time source; <see langword="null"/> uses the system clock.</param>
    public RealMetalAnalyserInnovX(MetalAnalyserOptions? options, TimeProvider? timeProvider = null)
        : base(DeviceKeys.MetalAnalyser, "XRF metal analyser (Innov-X COM)", isCritical: true, timeProvider)
    {
        _options = options ?? new MetalAnalyserOptions();
    }

    /// <inheritdoc />
    /// <remarks>Never raised by the Innov-X shell; the Vanta driver provides live progress.</remarks>
    public event EventHandler<AnalysisProgress>? ProgressChanged
    {
        add
        {
            // Intentionally empty: the Innov-X COM API exposes no progress stream.
        }
        remove
        {
            // Intentionally empty: the Innov-X COM API exposes no progress stream.
        }
    }

    /// <inheritdoc />
    public async Task<AnalysisRun> StartAnalysisAsync(CancellationToken cancellationToken = default)
    {
        EnsureOperable();
        object instance = _comInstance ?? throw new InvalidOperationException("innovx_not_connected");
        SetHealth(DeviceState.Busy, "Analysis running.");
        try
        {
            AnalysisRun run = await Task.Run(() =>
            {
                try
                {
                    VendorSdkLoader.Invoke(instance, "StartTest");
                    return Failed("innovx_result_binding_pending_hardware_validation");
                }
                catch (Exception ex) when (ex is MissingMethodException
                    or System.Reflection.TargetInvocationException
                    or COMException
                    or InvalidOperationException)
                {
                    return Failed($"innovx_com_error:{ex.GetType().Name}");
                }
            }, cancellationToken).ConfigureAwait(false);

            SetHealth(DeviceState.Ready);
            return run;
        }
        catch (OperationCanceledException)
        {
            SetHealth(DeviceState.Ready);
            throw;
        }
    }

    /// <inheritdoc />
    protected override Task ConnectCoreAsync(CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            Type comType = VendorSdkLoader.GetComType(_options.InnovXProgId);
            _comInstance = Activator.CreateInstance(comType);
        }, cancellationToken);

    /// <inheritdoc />
    protected override Task DisconnectCoreAsync(CancellationToken cancellationToken)
    {
        ReleaseComInstance();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ReleaseComInstance();
        }

        base.Dispose(disposing);
    }

    private static AnalysisRun Failed(string reason) => new(
        Succeeded: false,
        FailureReason: reason,
        GoldPercent: null,
        SilverPercent: null,
        GoldPlated: false,
        Elements: new Dictionary<string, decimal>());

    private void ReleaseComInstance()
    {
        if (_comInstance is not null && Marshal.IsComObject(_comInstance))
        {
            Marshal.ReleaseComObject(_comInstance);
        }

        _comInstance = null;
    }
}
