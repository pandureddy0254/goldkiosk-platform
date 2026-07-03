using GoldKiosk.Kiosk.Devices.Configuration;

namespace GoldKiosk.Kiosk.Devices.Real.Protocol;

/// <summary>
/// The Boyle's-law two-pressure volume computation, ported <b>verbatim</b> from the legacy
/// production path (<c>GoldCube.Client.cs</c> → <c>MVolume</c> with <c>Measure = true</c>):
/// <c>v2 = ΔV / (1 − p1·t2 / (p2·t1))</c>, item volume <c>= MtLgChamber − (v2 − ΔV)</c>.
/// Pressures are absolute psi (gauge average + ambient offset); temperatures cancel when
/// <c>t1 == t2</c> but are kept so a future differential-temperature calibration stays a
/// config change. See <see cref="VolumeChamberOptions"/> for the constants and the
/// config-vs-code parity notes (cup volume is folded into <c>MtLgChamber</c> at calibration
/// and is <b>not</b> subtracted here).
/// </summary>
public static class BoylesLawVolume
{
    /// <summary>Computes the item volume in cc from the two averaged pressure plateaus.</summary>
    /// <param name="sealedPressurePsi">Absolute pressure p1 after sealing, before compression.</param>
    /// <param name="compressedPressurePsi">Absolute pressure p2 with the piston up.</param>
    /// <param name="calibration">Chamber calibration constants.</param>
    /// <returns>The measured item volume in cc.</returns>
    /// <exception cref="InvalidOperationException">The two plateaus are degenerate (equal or non-positive), so no volume exists.</exception>
    public static decimal ComputeItemVolumeCc(
        decimal sealedPressurePsi,
        decimal compressedPressurePsi,
        VolumeChamberOptions calibration)
    {
        ArgumentNullException.ThrowIfNull(calibration);

        if (sealedPressurePsi <= 0m || compressedPressurePsi <= 0m)
        {
            throw new InvalidOperationException("volume_pressure_not_positive");
        }

        decimal denominator = 1m
            - (sealedPressurePsi * calibration.Temperature2)
            / (compressedPressurePsi * calibration.Temperature1);
        if (denominator == 0m)
        {
            throw new InvalidOperationException("volume_pressure_plateaus_equal");
        }

        decimal v2 = calibration.DeltaV / denominator;
        return calibration.MtLgChamber - (v2 - calibration.DeltaV);
    }

    /// <summary>
    /// Whether the measured pressure delta is within tolerance of the delta recorded at
    /// empty-chamber calibration (<c>VolumeReading.Calibrated</c>). The legacy form displayed
    /// <c>deltaP − CalDeltaP</c> without gating; the explicit gate is new.
    /// </summary>
    /// <param name="sealedPressurePsi">Absolute pressure p1.</param>
    /// <param name="compressedPressurePsi">Absolute pressure p2.</param>
    /// <param name="calibration">Chamber calibration constants.</param>
    /// <returns><see langword="true"/> when the chamber constants still describe the hardware.</returns>
    public static bool IsWithinCalibration(
        decimal sealedPressurePsi,
        decimal compressedPressurePsi,
        VolumeChamberOptions calibration)
    {
        ArgumentNullException.ThrowIfNull(calibration);

        decimal deltaP = compressedPressurePsi - sealedPressurePsi;
        return Math.Abs(deltaP - calibration.CalDeltaP) <= calibration.CalibrationDeltaTolerance;
    }
}
