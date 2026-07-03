using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Calibration constants and timing for the real volume chamber (Boyle's-law two-pressure
/// measurement: pressure sensor on COM4 + stepper board on COM3).
/// </summary>
/// <remarks>
/// <para><b>Config-vs-code parity note (legacy discrepancies, preserved deliberately).</b>
/// The legacy hardware sidecar defined these constants twice and the two sources disagreed:</para>
/// <list type="bullet">
/// <item><description><c>GoldCube.Client.cs</c> form fields hard-coded
/// <c>MtLgChamber = 840.5574</c>, <c>CupVolume = 46.2</c>, <c>CalDeltaP = 10</c>, and used
/// <c>deltaV = 590.385</c> in the older <c>MeasureLarge</c>/<c>MT_Chamber_Cal</c> paths but
/// <c>deltaV = 483.4911</c> in the production <c>MVolume</c> path.</description></item>
/// <item><description><c>ClientConfigProvider</c> (app.config) declared
/// <c>MtLgChamber = 735.0</c>, <c>CupVolume = 50.951</c>, <c>CalDeltaP = 10.0</c> — but the
/// form never read them, so the config values were dead.</description></item>
/// </list>
/// <para>This options class adopts the <em>config</em> constants (735.0 / 50.951 / 10.0) with
/// the <em>production-path</em> piston displacement (483.4911), per the GK-2 parity decision:
/// the config values are what field engineers were told they were tuning. Machines calibrated
/// against the old hard-coded values must set these keys during provisioning.</para>
/// <para>The production measurement path does <b>not</b> subtract <see cref="CupVolume"/>:
/// legacy empty-chamber calibration folded the cup into <see cref="MtLgChamber"/>
/// (<c>MtLgChamber -= emptyVolume + CupVolume</c>) and the per-item measurement then used the
/// adjusted constant unmodified.</para>
/// </remarks>
public sealed class VolumeChamberOptions
{
    /// <summary>Piston displacement volume in cc (legacy production path <c>MVolume</c>: 483.4911).</summary>
    [Range(1.0, 10_000.0)]
    public decimal DeltaV { get; set; } = 483.4911m;

    /// <summary>Calibrated empty-chamber volume in cc, cup folded in (legacy config default 735.0).</summary>
    [Range(1.0, 10_000.0)]
    public decimal MtLgChamber { get; set; } = 735.0m;

    /// <summary>Measurement cup volume in cc, used during empty-chamber calibration (legacy config default 50.951).</summary>
    [Range(0.0, 1000.0)]
    public decimal CupVolume { get; set; } = 50.951m;

    /// <summary>Expected pressure delta (psi) recorded at empty-chamber calibration (legacy config default 10.0).</summary>
    [Range(0.0, 1000.0)]
    public decimal CalDeltaP { get; set; } = 10.0m;

    /// <summary>
    /// Allowed drift between the measured pressure delta and <see cref="CalDeltaP"/> before a
    /// reading is flagged uncalibrated (<c>VolumeReading.Calibrated = false</c>). The legacy
    /// form displayed <c>deltaP - CalDeltaP</c> but never gated on it; the gate is new.
    /// </summary>
    [Range(0.0, 100.0)]
    public decimal CalibrationDeltaTolerance { get; set; } = 1.0m;

    /// <summary>Ambient offset added to gauge pressure readings, in psi (legacy constant 14.7).</summary>
    [Range(0.0, 100.0)]
    public decimal AmbientPressurePsi { get; set; } = 14.7m;

    /// <summary>Chamber temperature constant T1 (legacy constant 23; T1 == T2 so it cancels).</summary>
    [Range(1, 400)]
    public int Temperature1 { get; set; } = 23;

    /// <summary>Chamber temperature constant T2 (legacy constant 23).</summary>
    [Range(1, 400)]
    public int Temperature2 { get; set; } = 23;

    /// <summary>
    /// Pressure samples averaged per plateau. Legacy sampled <c>NumSample/2 = 200</c> in the
    /// production path and 50 in the older paths; the supported range is 50–400.
    /// </summary>
    [Range(50, 400)]
    public int SampleCount { get; set; } = 200;

    /// <summary>Dwell after each stepper command ack (legacy 2000 ms after <c>cu</c>/<c>cd</c>/<c>pu</c>/<c>pd</c>).</summary>
    [Range(0, 30_000)]
    public int CommandDwellMs { get; set; } = 2000;

    /// <summary>Settle time after sealing the chamber before the first plateau (legacy 20 000 ms).</summary>
    [Range(0, 120_000)]
    public int SealSettleDelayMs { get; set; } = 20_000;

    /// <summary>Settle time after piston up before the second plateau (legacy 47 500 ms).</summary>
    [Range(0, 120_000)]
    public int PistonSettleDelayMs { get; set; } = 47_500;

    /// <summary>Timeout waiting for the stepper board's <c>*</c> ack per command.</summary>
    [Range(100, 60_000)]
    public int AckTimeoutMs { get; set; } = 5000;

    /// <summary>Pressure sensor serial port (legacy COM4; a second link beside the shared stepper port).</summary>
    [Required(AllowEmptyStrings = false)]
    public string PressurePort { get; set; } = RealDeviceDefaults.PressureSensorPort;

    /// <summary>Pressure sensor baud rate (legacy 115200).</summary>
    [Range(300, 1_000_000)]
    public int PressureBaudRate { get; set; } = RealDeviceDefaults.PressureSensorBaudRate;

    /// <summary>Pressure sensor read timeout (legacy read with no timeout; now always finite).</summary>
    [Range(100, 60_000)]
    public int PressureReadTimeoutMs { get; set; } = 3000;
}
