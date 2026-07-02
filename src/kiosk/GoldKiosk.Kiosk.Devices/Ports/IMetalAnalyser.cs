using GoldKiosk.Kiosk.Devices.Abstractions;

namespace GoldKiosk.Kiosk.Devices.Ports;

/// <summary>
/// XRF metal analyser port. Real targets behind this one port: Olympus Vanta
/// (WebSocket JSON API + UDP heartbeat, method <c>preciousMetal-VLW</c>) and the older
/// Innov-X COM SDK — the composition root picks the driver for the fitted gun.
/// </summary>
public interface IMetalAnalyser : IKioskDevice
{
    /// <summary>Raised as the analysis advances (percent + stage) for UI progress display.</summary>
    event EventHandler<AnalysisProgress>? ProgressChanged;

    /// <summary>Runs a full composition analysis of the item currently positioned on the analyser.</summary>
    /// <param name="cancellationToken">Cancels the analysis run.</param>
    /// <returns>The completed run, successful or failed, with elemental composition when available.</returns>
    Task<AnalysisRun> StartAnalysisAsync(CancellationToken cancellationToken = default);
}
