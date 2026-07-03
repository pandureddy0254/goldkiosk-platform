using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Tuning for the real MT-SICS scale driver, bound from
/// <c>Devices:Overrides:scale:Scale</c> (host binding) with legacy-parity defaults.
/// The legacy driver's fixed <c>Substring(3, 11)</c> parse and machine-culture
/// <c>Convert.ToDecimal</c> are deliberately not carried forward — see
/// <c>Real.Protocol.MtSicsWeightParser</c>.
/// </summary>
public sealed class ScaleOptions
{
    /// <summary>
    /// Weight-query command without terminator (legacy <c>GetWeightCmd</c> default <c>Q</c>).
    /// The driver appends <c>\r\n</c>.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string WeightCommand { get; set; } = "Q";

    /// <summary>Re-zero command without terminator (legacy <c>ZeroScaleCmd</c> default <c>Z</c>).</summary>
    [Required(AllowEmptyStrings = false)]
    public string ZeroCommand { get; set; } = "Z";

    /// <summary>
    /// Delay between issuing the weight command and reading the response, letting the pan
    /// settle (legacy hard-coded <c>Task.Delay(2000)</c>). Configurable per machine.
    /// </summary>
    [Range(0, 30_000)]
    public int SettleDelayMs { get; set; } = 2000;

    /// <summary>Delay after a zero command before the scale is trusted again (legacy 3000 ms).</summary>
    [Range(0, 30_000)]
    public int ZeroSettleDelayMs { get; set; } = 3000;

    /// <summary>
    /// Serial read timeout. The legacy driver read with an infinite timeout and could hang the
    /// hardware sidecar forever; the port is now always opened with a finite timeout.
    /// </summary>
    [Range(100, 60_000)]
    public int ReadTimeoutMs { get; set; } = 5000;
}
