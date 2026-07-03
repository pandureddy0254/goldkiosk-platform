using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Kiosk.Devices.Configuration;

/// <summary>
/// Tuning for the real FlexCode fingerprint scanner. The device credentials
/// (serial / verification / activation codes) are per-unit licensing facts delivered through
/// config layer 4 — the legacy values baked into app.config are treated as exposed.
/// </summary>
public sealed class FingerprintScannerOptions
{
    /// <summary>FlexCode device serial number (legacy <c>FPScannerSerial</c>).</summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>FlexCode verification code (legacy <c>FPScannerVerificationCode</c>).</summary>
    public string VerificationCode { get; set; } = string.Empty;

    /// <summary>FlexCode activation code (legacy <c>FPScannerActivationCode</c>).</summary>
    public string ActivationCode { get; set; } = string.Empty;

    /// <summary>
    /// Registration secret passed to <c>FPRegistrationStart</c>. The legacy sidecar hard-coded
    /// <c>MySecretKey</c>; provisioning should set a per-tenant value.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string RegistrationSecret { get; set; } = "MySecretKey";

    /// <summary>Timeout for one capture attempt (finger placement through template/failure event).</summary>
    [Range(1000, 300_000)]
    public int CaptureTimeoutMs { get; set; } = 30_000;
}
