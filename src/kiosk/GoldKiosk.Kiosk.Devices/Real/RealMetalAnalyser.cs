using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Real;

/// <summary>
/// Real XRF analyser driver stub. Phase 4 target SDKs behind this one port: Olympus Vanta
/// (WebSocket JSON API + UDP heartbeat, method <c>preciousMetal-VLW</c>) and the older
/// Innov-X COM SDK; the composition root selects the driver for the fitted gun.
/// </summary>
public sealed class RealMetalAnalyser : RealDeviceStub, IMetalAnalyser
{
    /// <summary>Initializes the stub.</summary>
    public RealMetalAnalyser()
        : base(DeviceKeys.MetalAnalyser, "XRF metal analyser (Vanta WS / Innov-X COM)", isCritical: true)
    {
    }

    /// <inheritdoc />
    /// <remarks>Never raised by the stub; Phase 4 wires it to analyser progress callbacks.</remarks>
    public event EventHandler<AnalysisProgress>? ProgressChanged
    {
        add
        {
            // Intentionally empty: the stub never raises progress.
        }
        remove
        {
            // Intentionally empty: the stub never raises progress.
        }
    }

    /// <inheritdoc />
    public Task<AnalysisRun> StartAnalysisAsync(CancellationToken cancellationToken = default) =>
        throw NotWired();
}
