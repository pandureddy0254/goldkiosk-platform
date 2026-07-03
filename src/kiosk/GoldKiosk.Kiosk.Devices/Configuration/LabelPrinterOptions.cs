using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Tuning for the real Brother b-PAC label printer. The template's text object names are the
/// legacy <c>ShipForm.lbx</c> contract: <c>Text01</c> invoice, <c>Text02</c> bag number,
/// <c>Text03</c> weight, <c>Text04</c> offer display.
/// </summary>
public sealed class LabelPrinterOptions
{
    /// <summary>
    /// Path to the <c>.lbx</c> label template. Relative paths resolve against the application
    /// base directory (the legacy sidecar shipped <c>ShipForm.lbx</c> beside the exe).
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string TemplatePath { get; set; } = "ShipForm.lbx";

    /// <summary>COM ProgID of the b-PAC document class.</summary>
    [Required(AllowEmptyStrings = false)]
    public string ProgId { get; set; } = RealDeviceDefaults.BrotherBpacProgId;
}
