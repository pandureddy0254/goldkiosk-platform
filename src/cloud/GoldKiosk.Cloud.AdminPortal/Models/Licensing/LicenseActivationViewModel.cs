using System.ComponentModel.DataAnnotations;

namespace GoldKiosk.Cloud.AdminPortal.Models.Licensing;

/// <summary>License activation view model.</summary>
public sealed class LicenseActivationViewModel
{
    /// <summary>Gets or sets the license token.</summary>
    [Required(ErrorMessage = "Paste the AIKI- license token from your activation email.")]
    [Display(Name = "License token")]
    public string LicenseToken { get; set; } = "";
}
