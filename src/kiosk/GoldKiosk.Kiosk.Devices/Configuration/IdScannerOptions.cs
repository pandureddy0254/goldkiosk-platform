namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Tuning for the real ID scanner drivers. The fitted scanner is selected by
/// <c>Devices:Overrides:id_scanner:Connection:Variant</c> (<c>acuant</c> | <c>gemalto</c>).
/// </summary>
public sealed class IdScannerOptions
{
    /// <summary>
    /// Acuant LightSDK licence key. Delivered through config layer 4 (Key Vault via device
    /// identity) — the legacy key baked into app.config is treated as exposed and rotated.
    /// </summary>
    public string AcuantLicenseKey { get; set; } = string.Empty;

    /// <summary>
    /// Vendor SDK assembly for the <c>gemalto</c> variant (the <c>Connection</c> section's
    /// <c>VendorAssemblyPath</c> default targets the Acuant variant).
    /// </summary>
    public string GemaltoAssemblyPath { get; set; } = RealDeviceDefaults.GemaltoAssembly;
}
