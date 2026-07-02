using GoldKiosk.Kiosk.Devices.Abstractions;
using GoldKiosk.Kiosk.Devices.Configuration;
using GoldKiosk.Kiosk.Devices.Ports;

namespace GoldKiosk.Kiosk.Devices.Simulation;

/// <summary>
/// Simulated XRF analyser: emits deterministic progress (10→100) across the UI checklist
/// stages <c>item_detected</c> → <c>authenticating</c> → <c>pricing</c>, then returns a
/// 22-karat result (Au 91.6%, Ag 0.4%, Cu 8.0%, not plated) coherent with the simulated
/// scale and volume chamber.
/// </summary>
public sealed class SimulatedMetalAnalyser : SimulatedDeviceBase, IMetalAnalyser
{
    private static readonly (int Percent, string Stage)[] _progressScript =
    [
        (10, "item_detected"),
        (30, "item_detected"),
        (50, "authenticating"),
        (70, "authenticating"),
        (90, "pricing"),
        (100, "pricing"),
    ];

    /// <summary>Initializes the simulated analyser.</summary>
    /// <param name="simulation">Simulation tuning (latency, fault injection).</param>
    /// <param name="timeProvider">Time source for delays.</param>
    public SimulatedMetalAnalyser(SimulationOptions simulation, TimeProvider timeProvider)
        : base(DeviceKeys.MetalAnalyser, "XRF metal analyser (simulated)", isCritical: true, simulation, timeProvider)
    {
    }

    /// <inheritdoc />
    public event EventHandler<AnalysisProgress>? ProgressChanged;

    /// <inheritdoc />
    public async Task<AnalysisRun> StartAnalysisAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfFaulted();
        SetHealth(DeviceState.Busy, "Analysis in progress.");
        try
        {
            foreach ((int percent, string stage) in _progressScript)
            {
                await DelayAsync(500, cancellationToken).ConfigureAwait(false);
                ProgressChanged?.Invoke(this, new AnalysisProgress(percent, stage));
            }

            var elements = new Dictionary<string, decimal>(StringComparer.Ordinal)
            {
                ["Au"] = 91.6m,
                ["Ag"] = 0.4m,
                ["Cu"] = 8.0m,
            };

            return new AnalysisRun(
                Succeeded: true,
                FailureReason: null,
                GoldPercent: 91.6m,
                SilverPercent: 0.4m,
                GoldPlated: false,
                Elements: elements);
        }
        finally
        {
            SetHealth(DeviceState.Ready);
        }
    }
}
