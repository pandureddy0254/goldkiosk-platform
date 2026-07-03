using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Waypoints and motion tuning for the real robotic arm (Dobot CR over TCP plus the optional
/// Kollmorgen AKD linear axis used for analyser positioning). Waypoints are
/// <c>x:y:z:r</c> strings in Dobot Cartesian coordinates; a trailing fifth legacy component
/// is tolerated and ignored. Defaults are the legacy <c>ConfigProvider</c> point table.
/// </summary>
public sealed class RoboticArmOptions
{
    /// <summary>Safe home pose (legacy <c>HomePoint</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    public string HomePoint { get; set; } = "128.37:-176.9:163.41:-14.16";

    /// <summary>Scale pan drop/pick pose (legacy <c>ScalePoint</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    public string ScalePoint { get; set; } = "125.47:-280.9:-44:-25.30";

    /// <summary>XRF analyser window pose (legacy <c>XrayPoint</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    public string XrayPoint { get; set; } = "246.80:-61.60:143.58:26.74";

    /// <summary>Volume chamber pose (legacy <c>ChamberPoint</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    public string ChamberPoint { get; set; } = "228.17:75.652:152.93:61.763";

    /// <summary>Bagging position pose (legacy <c>BagPoint</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    public string BagPoint { get; set; } = "59.640:-210.6:123.99:-32.90";

    /// <summary>Mid-air safe point between home and scale (legacy <c>MidSafePointScaleAndHome</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    public string MidSafePointScaleAndHome { get; set; } = "108.68:-196.2:19.796:-23.57";

    /// <summary>Mid-air safe point between home and the analyser (legacy <c>MidSafePointXrayAndHome</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    public string MidSafePointXrayAndHome { get; set; } = "211.38:-55.89:163.41:27.565";

    /// <summary>Mid-air safe point between home and the chamber (legacy <c>MidSafePointChamberAndHome</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    public string MidSafePointChamberAndHome { get; set; } = "232.26:-61.96:163.52:28.48";

    /// <summary>
    /// Timeout for one waypoint's motion to complete, confirmed via the feedback stream's
    /// robot-mode transition (replaces the legacy blind <c>Task.Delay(500)</c>).
    /// </summary>
    [Range(1000, 300_000)]
    public int MotionTimeoutMs { get; set; } = 30_000;

    /// <summary>Feedback poll interval while waiting for motion completion.</summary>
    [Range(20, 5000)]
    public int FeedbackPollIntervalMs { get; set; } = 100;

    /// <summary>
    /// Grace period after issuing a move during which the arm may still report idle (short
    /// moves can finish before the feedback stream shows <c>Running</c>).
    /// </summary>
    [Range(0, 10_000)]
    public int MotionStartGraceMs { get; set; } = 1500;

    /// <summary>Whether the AKD linear axis participates in analyser positioning moves.</summary>
    public bool AkdEnabled { get; set; }

    /// <summary>Kollmorgen AKD drive host (legacy <c>AKD.AmpsLocalAddressZ</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    public string AkdHost { get; set; } = RealDeviceDefaults.AkdHost;

    /// <summary>Kollmorgen AKD telnet port.</summary>
    [Range(1, 65_535)]
    public int AkdPort { get; set; } = RealDeviceDefaults.AkdPort;

    /// <summary>Motion-task number issued via <c>MT.MOVE</c> to position the axis at the analyser.</summary>
    [Range(0, 128)]
    public int AkdAnalyserMotionTask { get; set; }

    /// <summary>Timeout for an AKD axis move (poll of <c>DRV.MOTIONSTAT</c>).</summary>
    [Range(1000, 300_000)]
    public int AkdMotionTimeoutMs { get; set; } = 30_000;
}
